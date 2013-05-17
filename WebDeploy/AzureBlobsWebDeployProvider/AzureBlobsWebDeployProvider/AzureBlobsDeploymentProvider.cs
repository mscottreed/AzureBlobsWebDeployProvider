namespace AzureBlobsWebDeployProvider
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography;

    using Microsoft.Web.Deployment;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.StorageClient;

    //using Microsoft.WindowsAzure;
    //using Microsoft.WindowsAzure.StorageClient;

    public class AzureBlobsDeploymentProvider : DeploymentObjectProvider
    {
        internal const string ProviderName = "azureBlob";
        internal const string KeyAttributeName = "path";
        internal HashSet<string> files = new HashSet<string>();

        public AzureBlobsDeploymentProvider(DeploymentProviderContext providerContext, DeploymentBaseContext baseContext)
            : base(providerContext, baseContext)
        {
            DirPath = providerContext.Path;
        }

        public override DeploymentObjectAttributeData CreateKeyAttributeData()
        {
            var attributeData = new DeploymentObjectAttributeData(
                KeyAttributeName,
                DirPath,
                DeploymentObjectAttributeKind.CaseInsensitiveCompare);
            return attributeData;
        }

        public override string Name
        {
            get { return ProviderName; }
        }

        public string DirPath { get; set; }

        public override void GetAttributes(DeploymentAddAttributeContext addContext)
        {
            if (this.BaseContext.IsDestinationObject)
            {
                throw new DeploymentException();
            }
            // We are acting on the source object here, make sure that the file exists on disk
            if (!Directory.Exists(this.DirPath))
            {
                string message = string.Format("Directory <{0}> does not exist", this.DirPath);
                throw new DeploymentFatalException(message);
            }

            base.GetAttributes(addContext);
        }


        public override void Add(DeploymentObject source, bool whatIf)
        {
            // This is called on the Destination so this.FilePath is the dest path not source path
            if (Directory.Exists(source.ProviderContext.Path))
            {
                var rootDirInfo = new DirectoryInfo(source.ProviderContext.Path);
                // get that directory and and make sure that a container name exists in the cloud
                var container = GetContainer(rootDirInfo.Name);
                Console.WriteLine("Creating container {0}", rootDirInfo.Name);
                if (!whatIf)
                {
                    container.CreateIfNotExist();
                }

                // Loop over items within the container and output the length and URI.
                foreach (IListBlobItem item in container.ListBlobs(new BlobRequestOptions { UseFlatBlobListing = true }))
                {
                    if (item is CloudBlockBlob)
                    {
                        files.Add(item.Uri.AbsolutePath);
                    }
                }

                SyncFiles(whatIf, container, "", rootDirInfo);
            }
        }

        private void SyncFiles(bool whatIf, CloudBlobContainer container, string prefix, DirectoryInfo dirInfo)
        {
            if (prefix != "") prefix += "/";
            foreach (var file in dirInfo.GetFiles())
            {
                UploadBlob(whatIf, container, prefix, file);
            }
            foreach (var childDirInfo in dirInfo.GetDirectories())
            {
                var childDirName = childDirInfo.Name.ToLower();
                SyncFiles(whatIf, container, prefix + childDirName, childDirInfo);
            }
        }

        private void UploadBlob(bool whatIf, CloudBlobContainer container, string prefix, FileInfo file)
        {
            string path = (prefix + file.Name).ToLower();
            var uriPath = "/" + container.Name + "/" + path;
            bool shouldUpload = false;
            CloudBlockBlob blob = container.GetBlockBlobReference(path);
            if (!files.Contains(uriPath))
            {
                Console.WriteLine("File not present, uploading {0}", path);
                shouldUpload = true;
            }
            else
            {
                files.Remove(uriPath);
                blob.FetchAttributes();
                var originalHash = blob.Properties.ContentMD5;
                using (var fileStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
                {
                    using (var md5gen = new MD5CryptoServiceProvider())
                    {
                        md5gen.ComputeHash(fileStream);
                        var newHash = Convert.ToBase64String(md5gen.Hash);
                        if (originalHash != newHash)
                        {
                            Console.WriteLine("Hashes did not match, uploading {0}", path);
                            shouldUpload = true;
                        }
                        else
                        {
                            Console.WriteLine("Hashes matched, ignoring {0}", path);
                        }
                    }
                }
            }
            if (shouldUpload)
            {
                if (!whatIf)
                {
                    using (var fileStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
                    {
                        blob.UploadFromStream(fileStream);
                    }
                }
            }
        }

        public override void Update(DeploymentObject source, bool whatIf)
        {
            // No support for update, just call Add again
            Add(source, whatIf);
        }

        protected virtual CloudBlobContainer GetContainer(string containerName)
        {
            return GetClient().GetContainerReference(containerName.ToLower());
        }

        protected virtual CloudBlobClient GetClient()
        {
            var account = CloudStorageAccount.Parse(DirPath);
            return account.CreateCloudBlobClient();
        }

    }
}
