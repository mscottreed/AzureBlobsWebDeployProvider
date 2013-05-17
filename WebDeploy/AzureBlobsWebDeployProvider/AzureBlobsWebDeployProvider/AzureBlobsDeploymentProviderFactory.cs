namespace AzureBlobsWebDeployProvider
{
    using Microsoft.Web.Deployment;

    [DeploymentProviderFactory]
    public class AzureBlobsDeploymentProviderFactory : DeploymentProviderFactory
    {
        protected override DeploymentObjectProvider Create(DeploymentProviderContext providerContext, DeploymentBaseContext baseContext)
        {
            return new AzureBlobsDeploymentProvider(providerContext, baseContext);
        }

        public override string ExamplePath
        {
            get { return "DefaultEndpointsProtocol=https;AccountName=someaccount;AccountKey=SomeReallyLongBase64encodedString"; }
        }

        public override string FriendlyName
        {
            get { return "Azure Blobs deployment provider"; }
        }

        public override string Name
        {
            get { return AzureBlobsDeploymentProvider.ProviderName; }
        }
    }
}
