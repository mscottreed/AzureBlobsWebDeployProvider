# First run a whatif so see which files *would* be deployed (but don't deploy them)
# this is a really just an optimization, we *could* do a search and replace on every single file
& 'C:\Program Files\IIS\Microsoft Web Deploy\msdeploy.exe' "-whatIf" "-verb:sync" "-source:contentPath='L:\'" "-dest:contentPath='illuminatest/',ComputerName='https://waws-prod-bay-001.publish.azurewebsites.windows.net/msdeploy.axd?site=illuminatest',UserName='AdminScott',Password='7yuihjk.',AuthType='Basic'" "-enableRule:DoNotDeleteRule" "-skip:objectName=dirPath,absolutePath=Documents" "-skip:objectName=dirPath,absolutePath=Images" "-skip:objectName=dirPath,absolutePath=Media" > whatif.txt

# Take the files that will be sent and do some CDN path replacement on them
$matches = Select-String "Info: (Add|Updat)ing (child )?filePath \(illuminatest/(.*)\)" .\whatif.txt
$matches | Select -Expand Matches | Foreach { "L:\" + $_.Groups[3].Value } > .\filesChanged.txt
Get-Content .\filesChanged.txt | Foreach-Object { 
   $file = $_; `
   if ($_.EndsWith(".css")) { 
      (Get-Content $file) | 
      Foreach-Object {$_ -replace "src=../images", "src=http://az413034.vo.msecnd.net/images" } |
      Set-Content $file
   } elseif ($_.EndsWith(".js")) { 
      (Get-Content $file) | 
      Foreach-Object {$_ -replace "src=`"../images", "src=`"http://az413034.vo.msecnd.net/images" } |
      Set-Content $file
   } elseif ($_.EndsWith(".ilmn")) { 
      (Get-Content $file) | 
      Foreach-Object {$_ -replace "src=`"/?images", "src=`"http://az413034.vo.msecnd.net/images" } |
      Foreach-Object {$_ -replace "src=`"/?documents", "src=`"http://az413034.vo.msecnd.net/documents" } |
      Foreach-Object {$_ -replace "src=`"/?media", "src=`"http://az413034.vo.msecnd.net/media" } |
      Set-Content $file
   } elseif ($_.EndsWith(".aspx")) { 
      (Get-Content $file) | 
      Foreach-Object {$_ -replace "src=`"/?images", "src=`"http://az413034.vo.msecnd.net/images" } |
      Foreach-Object {$_ -replace "src=`"/?documents", "src=`"http://az413034.vo.msecnd.net/documents" } |
      Foreach-Object {$_ -replace "src=`"/?media", "src=`"http://az413034.vo.msecnd.net/media" } |
      Set-Content $file
   } 
}

# Upload the three sets of files into the CDN
& 'C:\Program Files\IIS\Microsoft Web Deploy\msdeploy.exe' "-verb:sync" "-source:azureBlob='L:\Documents'" "-dest:azureBlob='DefaultEndpointsProtocol=https;AccountName=ilmntestcdn;AccountKey=KBI+COYeCoS85b2XYpfn/5eA6rtnSYbO0lTd9cAspNhxkM2q9M3IIekYwkxOPrR2TRXGVXEqggi5ChXRshAfgw=='"
& 'C:\Program Files\IIS\Microsoft Web Deploy\msdeploy.exe' "-verb:sync" "-source:azureBlob='L:\Images'" "-dest:azureBlob='DefaultEndpointsProtocol=https;AccountName=ilmntestcdn;AccountKey=KBI+COYeCoS85b2XYpfn/5eA6rtnSYbO0lTd9cAspNhxkM2q9M3IIekYwkxOPrR2TRXGVXEqggi5ChXRshAfgw=='"
& 'C:\Program Files\IIS\Microsoft Web Deploy\msdeploy.exe' "-verb:sync" "-source:azureBlob='L:\Media'" "-dest:azureBlob='DefaultEndpointsProtocol=https;AccountName=ilmntestcdn;AccountKey=KBI+COYeCoS85b2XYpfn/5eA6rtnSYbO0lTd9cAspNhxkM2q9M3IIekYwkxOPrR2TRXGVXEqggi5ChXRshAfgw=='"

# Now go ahead and deploy the actual content
& 'C:\Program Files\IIS\Microsoft Web Deploy\msdeploy.exe' "-verb:sync" "-source:contentPath='L:\'" "-dest:contentPath='illuminatest/',ComputerName='https://waws-prod-bay-001.publish.azurewebsites.windows.net/msdeploy.axd?site=illuminatest',UserName='AdminScott',Password='7yuihjk.',AuthType='Basic'" "-enableRule:DoNotDeleteRule" "-skip:objectName=dirPath,absolutePath=Documents" "-skip:objectName=dirPath,absolutePath=Images" "-skip:objectName=dirPath,absolutePath=Media"
