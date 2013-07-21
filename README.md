AzureBlobsWebDeployProvider
===========================

Web Deploy provider for Azure Blob Storage

Install is included in the post-build step.  

Need to have ILMerge installed.
http://www.microsoft.com/en-us/download/details.aspx?id=17630

Example of usage (line ending added for readability):
msdeploy.exe -verb:sync 
 -source:contentPath="C:\<path_to_blobs>" 
 -dest:contentPath='<Azure_blobs_container_name>',ComputerName="https://<waws-prod-bay-001>.publish.azurewebsites.windows.net/msdeploy.axd?site=<site_name>",UserName='<user_name>',Password='<password>',AuthType='Basic' 
 -enableRule:DoNotDeleteRule 
 -skip:objectName=dirPath,absolutePath=<subdirectory_under_path_to_blobs>
