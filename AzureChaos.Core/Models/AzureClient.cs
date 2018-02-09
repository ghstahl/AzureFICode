using AzureChaos.ReguestHelpers;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace AzureChaos.Models
{
    /// <summary>Azure configuration model which azure tenant, subscription
    /// and resource group needs to be crawled</summary>
    public class AzureClient
    {
        public IAzure azure;

        public AzureSettings azureSettings;

        /// <summary>For now, keeping the configuration information here.</summary>
        /// will be adding the storage account details in the azure function and will provide azure function
        public AzureClient()
        {
            ///Microsoft subscription blob endpoint for configs:  https://chaostest.blob.core.windows.net/config/azuresettings.json
            ///Zen3 subscription blob endpoint for configs: ==>  https://cmonkeylogs.blob.core.windows.net/configs/azuresettings.json
            /// Microsoft demo config file ==> https://stachaosteststorage.blob.core.windows.net/configs/azuresettings.json

            this.azureSettings = JsonConvert.DeserializeObject<AzureSettings>(HTTPHelpers.ExecuteGetWebRequest("https://cmnewschema.blob.core.windows.net/configs/azuresettings.json"));
            this.SubscriptionId = azureSettings.Client.SubscriptionId; //ConfigurationManager.AppSettings["SubscriptionId"];
            this.ClientId = azureSettings.Client.ClientId; //ConfigurationManager.AppSettings["ClientId"];
            this.ClientSecret = azureSettings.Client.ClientSecret; //ConfigurationManager.AppSettings["ClientSecret"];
            this.TenantId = azureSettings.Client.TenantId; //ConfigurationManager.AppSettings["TenantId"];
            this.Region = azureSettings.Client.Region; //ConfigurationManager.AppSettings["Region"];
            this.ResourceGroup = azureSettings.Client.ResourceGroup; //ConfigurationManager.AppSettings["ResourceGroup"];
            this.StorageAccountName = azureSettings.Client.StorageAccountName; //ConfigurationManager.AppSettings["StorageAccountName"];
            this.ResourceGroupCrawlerTableName = azureSettings.ResourceGroupCrawlerTableName; //ConfigurationManager.AppSettings["ResourceGroupCrawlerTableName"];
            this.VirtualMachineCrawlerTableName = azureSettings.VirtualMachineCrawlerTableName; //ConfigurationManager.AppSettings["VirtualMachineCrawlerTableName"];
            this.AvailabilitySetCrawlerTableName = azureSettings.AvailabilitySetCrawlerTableName; //ConfigurationManager.AppSettings["AvailabilitySetCrawlerTableName"];
            this.ScaleSetCrawlerTableName = azureSettings.ScaleSetCrawlerTableName; //ConfigurationManager.AppSettings["ScaleSetCrawlerTableName"];
            this.AvailabilityZoneCrawlerTableName = azureSettings.AvailabilityZoneCrawlerTableName; //ConfigurationManager.AppSettings["AvailabilityZoneCrawlerTableName"];
            this.ActivityLogTable = azureSettings.ActivityLogTable; //ConfigurationManager.AppSettings["ActivityLogTable"];
            this.ScheduledRulesTable = azureSettings.ScheduledRulesTable;
            this.EnableAvailabilitySet = azureSettings.Chaos.AvailabilitySetChaos.Enabled;
            this.EnableFaultDomain = azureSettings.Chaos.AvailabilitySetChaos.FaultDomainEnabled;
            this.EnableUpdateDomain = azureSettings.Chaos.AvailabilitySetChaos.UpdateDomainEnabled;
            this.EnableAvailabilityZone = azureSettings.Chaos.AvailabilityZoneChaos.Enabled;
            this.AvailabileZoneRegions = azureSettings.Chaos.AvailabilityZoneChaos.Regions;
            this.azure = GetAzure(this.ClientId, this.ClientSecret, this.TenantId);
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
        public string ScheduledRulesTable { get; set; }

        /// <summary>The Azure subscription id.</summary>
        public string StorageAccountName { get; set; }

        /// <summary>Is Availability Set Enabled for Rule Engine</summary>
        public bool EnableAvailabilitySet { get; set; }

        /// <summary>Is Fault Domain of Availability Set Enabled for Rule Engine</summary>
        public bool EnableFaultDomain { get; set; }

        /// <summary>Is Update Domain Availability Set Enabled for Rule Engine</summary>
        public bool EnableUpdateDomain { get; set; }

        /// <summary>Is Availability Zone Enabled for Rule Engine</summary>
        public bool EnableAvailabilityZone { get; set; }

        public List<string> AvailabileZoneRegions { get; set; }

        /// <summary>Get the Azure object to read the all resources from azure</summary>
        /// <returns>Returns the Azure object.</returns>
        private IAzure GetAzure(string clientId, string clientSecret, string tenantId)
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
                   .WithSubscription(this.SubscriptionId);
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
    }
}