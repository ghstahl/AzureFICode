using AzureChaos.ReguestHelpers;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Newtonsoft.Json;
using System;
using System.Configuration;

namespace AzureChaos.Models
{
    /// <summary>Azure configuration model which azure tenant, subscription 
    /// and resource group needs to be crawled</summary>
    public class AzureClient
    {
        public IAzure azure;

        /// <summary>For now, keeping the configuration information here.</summary>
        /// will be adding the storage account details in the azure function and will provide azure function
        public AzureClient()
        {
            ///Microsoft subscription blob endpoint for configs:  https://chaostest.blob.core.windows.net/config/azuresettings.json
            ///Zen3 subscription blob endpoint for configs:  https://cmonkeylogs.blob.core.windows.net/configs/azuresettings.json
            var clientConfig = JsonConvert.DeserializeObject<AzureSettings>(HTTPHelpers.ExecuteGetWebRequest("https://cmonkeylogs.blob.core.windows.net/configs/azuresettings.json"));
            this.SubscriptionId = clientConfig.SubscriptionId; //ConfigurationManager.AppSettings["SubscriptionId"];
            this.ClientId = clientConfig.ClientId; //ConfigurationManager.AppSettings["ClientId"];
            this.ClientSecret = clientConfig.ClientSecret; //ConfigurationManager.AppSettings["ClientSecret"];
            this.TenantId = clientConfig.TenantId; //ConfigurationManager.AppSettings["TenantId"];
            this.Region = clientConfig.Region; //ConfigurationManager.AppSettings["Region"];
            this.ResourceGroup = clientConfig.ResourceGroup; //ConfigurationManager.AppSettings["ResourceGroup"];
            this.ResourceGroupCrawlerTableName = clientConfig.ResourceGroupCrawlerTableName; //ConfigurationManager.AppSettings["ResourceGroupCrawlerTableName"];
            this.VirtualMachineCrawlerTableName = clientConfig.VirtualMachineCrawlerTableName; //ConfigurationManager.AppSettings["VirtualMachineCrawlerTableName"];
            this.AvailabilitySetCrawlerTableName = clientConfig.AvailabilitySetCrawlerTableName; //ConfigurationManager.AppSettings["AvailabilitySetCrawlerTableName"];
            this.ScaleSetCrawlerTableName = clientConfig.ScaleSetCrawlerTableName; //ConfigurationManager.AppSettings["ScaleSetCrawlerTableName"];
            this.AvailabilityZoneCrawlerTableName = clientConfig.AvailabilityZoneCrawlerTableName; //ConfigurationManager.AppSettings["AvailabilityZoneCrawlerTableName"];
            this.ActivityLogTable = clientConfig.ActivityLogTable; //ConfigurationManager.AppSettings["ActivityLogTable"];
            this.StorageAccountName = clientConfig.StorageAccountName; //ConfigurationManager.AppSettings["StorageAccountName"];
            this.azure = GetAzure();
        }

        /// <summary>The Azure subscription id.</summary>
        public string SubscriptionId { get; set; }

        /// <summary>The Azure subscription id.</summary>
        public string ClientId { get; set; }

        /// <summary>The Azure subscription id.</summary>
        public string ClientSecret { get; set; }

        /// <summary>The Azure subscription id.</summary>
        public string TenantId { get; set; }

        /// <summary>The Azure subscription id.</summary>
        public string Region { get; set; }

        /// <summary>The Azure subscription id.</summary>
        public string ResourceGroup { get; set; }

        /// <summary>The Azure subscription id.</summary>
        public string ResourceGroupCrawlerTableName { get; set; }

        /// <summary>The Azure subscription id.</summary>
        public string VirtualMachineCrawlerTableName { get; set; }

        /// <summary>The Azure subscription id.</summary>
        public string AvailabilitySetCrawlerTableName { get; set; }

        /// <summary>The Azure subscription id.</summary>
        public string ScaleSetCrawlerTableName { get; set; }

        /// <summary>The Azure subscription id.</summary>
        public string AvailabilityZoneCrawlerTableName { get; set; }

        /// <summary>The Azure subscription id.</summary>
        public string ActivityLogTable { get; set; }

        /// <summary>The Azure subscription id.</summary>
        public string StorageAccountName { get; set; }

        /// <summary>Get the Azure object to read the all resources from azure</summary>
        /// <returns>Returns the Azure object.</returns>
        private IAzure GetAzure()
        {
            IAzure azure = null;
            try
            {
                AzureCredentials azureCredentials = GetAzureCredentials();
                if (azureCredentials == null)
                {
                    return null;
                }

                azure = Azure
                   .Configure()
                   .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                   .Authenticate(azureCredentials)
                   .WithSubscription(this.SubscriptionId);
            }
            catch (Exception exception)
            {
                throw exception;
            }

            return azure;
        }

        /// <summary>Get azure credentials.</summary>
        /// <returns></returns>
        private AzureCredentials GetAzureCredentials()
        {
            return SdkContext.AzureCredentialsFactory
                            .FromServicePrincipal(this.ClientId, this.ClientSecret, this.TenantId, AzureEnvironment.AzureGlobalCloud);
        }
    }
}
