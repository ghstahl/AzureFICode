using AzureChaos.Core.AzureClient;
using AzureChaos.Enums;
using AzureChaos.ReguestHelpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace AzureChaos.Models
{
    /// <summary>Azure configuration model which azure tenant, subscription 
    /// and resource group needs to be crawled</summary>
    public class ADConfiguration
    {
        /// <summary>For now, keeping the configuration information here.</summary>
        /// will be adding the storage account details in the azure function and will provide azure function
        public ADConfiguration()
        {
            GlobalConfig config = JsonConvert.DeserializeObject<GlobalConfig>(HTTPHelpers.ExecuteGetWebRequest("https://cmonkeylogs.blob.core.windows.net/configs/accountConfig.json"));
            ResourceGroup = "Chaos_Monkey_RG";
            SubscriptionId = config.subscription;
            TenantId = config.tenant;
            Region = "EastUS";
            ClientId = config.client;
            ClientSecret = config.key;
        }

        /// <summary>Azure Authentication type i.e. Certificate based, credential based</summary>
        [JsonProperty("authtype")]
        public AuthenticationType AuthenticationType { get; set; }

        /// <summary>Azure client id</summary>
        [JsonProperty("clientid")]
        public string ClientId { get; set; }

        /// <summary>Azure client secret</summary>
        [JsonProperty("clientsecret")]
        public string ClientSecret { get; set; }

        /// <summary>Azure client certificate path</summary>
        [JsonProperty("certificatepath")]
        public string CertificatePath { get; set; }

        /// <summary>Azure client certificate password</summary>
        [JsonProperty("certificateassword")]
        public string CertificatePassword { get; set; }

        /// <summary>Defines the crawling region</summary>
        [JsonProperty("region")]
        public string Region { get; set; }

        /// <summary>Defines the crawling tenant</summary>
        [JsonProperty("tenantid")]
        public string TenantId { get; set; }

        /// <summary>Defines the crawling subscription</summary>
        [JsonProperty("subscriptionid")]
        public string SubscriptionId { get; set; } /*what happens when user wants to do chaos for multiple subscriptions?*/

        /// <summary>Defines the crawling resource group.</summary>
        [JsonProperty("resourcegroup")]
        public string ResourceGroup { get; set; }

        /// <summary>Defines the default resource group.</summary>
        [JsonProperty("resourcegroup")]
        public string DefaultResourceGroup { get; set; }

        /// <summary>Defines resource types which want to exclude from the chaos. its defined by comma separated.</summary>
        [JsonProperty("excludedresourcetype")]
        public string ExcludedResourceType { get; set; }       
        
    }
}
