using AzureChaos.Core.Models.Configs;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace AzureChaos.Core.Models
{
    /// <summary>Azure configuration model which azure tenant, subscription
    /// and resource group needs to be crawled</summary>
    public class AzureClient
    {
        public IAzure AzureInstance;

        public AzureSettings AzureSettings;

        /// <summary>For now, keeping the configuration information here.</summary>
        /// will be adding the storage account details in the azure function and will provide azure function
        public AzureClient()
        {
            //this.azureSettings = JsonConvert.DeserializeObject<AzureSettings>(HTTPHelpers.ExecuteGetWebRequest(Endpoints.ConfigEndpoint));
            AzureSettings = GetAzureSettings();
            if (AzureSettings != null)
                AzureInstance = GetAzure(AzureSettings.Client.ClientId, AzureSettings.Client.ClientSecret,
                    AzureSettings.Client.TenantId, AzureSettings.Client.SubscriptionId);
        }

        /// <summary>Get the Azure object to read the all resources from azure</summary>
        /// <returns>Returns the Azure object.</returns>
        private static IAzure GetAzure(string clientId, string clientSecret, string tenantId, string subscriptionId)
        {
            var azureCredentials = GetAzureCredentials(clientId, clientSecret, tenantId);
            if (azureCredentials == null)
            {
                return null;
            }

            var azure = Azure
                .Configure()
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .Authenticate(azureCredentials)
                .WithSubscription(subscriptionId);

            return azure;
        }

        /// <summary>Get azure credentials based on the client id and client secret.</summary>
        /// <returns></returns>
        private static AzureCredentials GetAzureCredentials(string clientId, string clientSecret, string tenantId)
        {
            return SdkContext.AzureCredentialsFactory
                            .FromServicePrincipal(clientId, clientSecret, tenantId, AzureEnvironment.AzureGlobalCloud);
        }

        private static AzureSettings GetAzureSettings()
        {
            // Zen3 - string ConnectionString = "DefaultEndpointsProtocol=https;AccountName=cmnewschema;AccountKey=Txyvz6P4vUvRBOMrPo8TWE6jtm6JS7PG0+l696iOAua4ZaPXjZhzHtPuFb+Zg8nb5SQLev2flNExlEs7KoimdQ==;EndpointSuffix=core.windows.net";
            // Microsft - string ConnectionString = "DefaultEndpointsProtocol=https;AccountName=cmnewschema;AccountKey=Txyvz6P4vUvRBOMrPo8TWE6jtm6JS7PG0+l696iOAua4ZaPXjZhzHtPuFb+Zg8nb5SQLev2flNExlEs7KoimdQ==;EndpointSuffix=core.windows.net";
            const string connectionString = "DefaultEndpointsProtocol=https;AccountName=cmnewschema;AccountKey=Txyvz6P4vUvRBOMrPo8TWE6jtm6JS7PG0+l696iOAua4ZaPXjZhzHtPuFb+Zg8nb5SQLev2flNExlEs7KoimdQ==;EndpointSuffix=core.windows.net";
            var storageAccount = CloudStorageAccount.Parse(connectionString);

            var blobClinet = storageAccount.CreateCloudBlobClient();
            var blobContainer = blobClinet.GetContainerReference("configs");
            var blobReference = blobContainer.GetBlockBlobReference("azuresettings.json");
            var tas = Task.Run(() => blobReference.DownloadTextAsync());
            var data = tas.Result;
            var azureSettings = JsonConvert.DeserializeObject<AzureSettings>(data);
            return azureSettings;
        }

    }
}
