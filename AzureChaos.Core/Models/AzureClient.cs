using AzureChaos.Providers;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Newtonsoft.Json;
using System;
using Microsoft.WindowsAzure.Storage;
using System.Threading.Tasks;

namespace AzureChaos.Models
{
    /// <summary>Azure configuration model which azure tenant, subscription
    /// and resource group needs to be crawled</summary>
    public class AzureClient
    {
        private static readonly IStorageAccountProvider StorageProvider = new StorageAccountProvider();
        public IAzure azure;

        public AzureSettings azureSettings;
        public int MeanTimeInHours = 2;

        /// <summary>For now, keeping the configuration information here.</summary>
        /// will be adding the storage account details in the azure function and will provide azure function
        public AzureClient()
        {
            //this.azureSettings = JsonConvert.DeserializeObject<AzureSettings>(HTTPHelpers.ExecuteGetWebRequest(Endpoints.ConfigEndpoint));
            this.azureSettings = GetAzureSettings();
            this.azure = GetAzure(azureSettings.Client.ClientId, azureSettings.Client.ClientSecret, azureSettings.Client.TenantId, azureSettings.Client.SubscriptionId);
        }

        /// <summary>Get the Azure object to read the all resources from azure</summary>
        /// <returns>Returns the Azure object.</returns>
        private IAzure GetAzure(string clientId, string clientSecret, string tenantId, string subscriptionId)
        {
            IAzure azure = null;
            try
            {
                AzureCredentials azureCredentials = GetAzureCredentials(clientId, clientSecret, tenantId);
                if (azureCredentials == null)
                {
                    return null;
                }

                azure = Azure
                   .Configure()
                   .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                   .Authenticate(azureCredentials)
                   .WithSubscription(subscriptionId);
            }
            catch (Exception exception)
            {
                throw exception;
            }

            return azure;
        }

        /// <summary>Get azure credentials based on the client id and client secret.</summary>
        /// <returns></returns>
        private AzureCredentials GetAzureCredentials(string clientId, string clientSecret, string tenantId)
        {
            return SdkContext.AzureCredentialsFactory
                            .FromServicePrincipal(clientId, clientSecret, tenantId, AzureEnvironment.AzureGlobalCloud);
        }

        private AzureSettings GetAzureSettings()
        {
            try
            {
                string ConnectionString = "DefaultEndpointsProtocol=https;AccountName=cmnewschema;AccountKey=Txyvz6P4vUvRBOMrPo8TWE6jtm6JS7PG0+l696iOAua4ZaPXjZhzHtPuFb+Zg8nb5SQLev2flNExlEs7KoimdQ==;EndpointSuffix=core.windows.net";
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConnectionString);

                var blobClinet = storageAccount.CreateCloudBlobClient();
                var blobContainer = blobClinet.GetContainerReference("configs");
                var blobReference = blobContainer.GetBlockBlobReference("azuresettings.json");
                var tas = Task.Run(() => blobReference.DownloadTextAsync());
                var data = tas.Result;
                var azureSettings = JsonConvert.DeserializeObject<AzureSettings>(data);
                return azureSettings;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

    }
}
