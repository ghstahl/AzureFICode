using AzureChaos.Enum;
using Newtonsoft.Json;

namespace AzureChaos.Models
{
    /// <summary>Azure configuration model which azure tenant, subscription 
    /// and resource group needs to be crawled</summary>
    public class ADConfiguration
    {
        /// <summary>Azure Authentication type i.e. Certificate based, credential based</summary>
        [JsonProperty("authtype")]
        public AuthenticationType AuthenticationType { get; set; }

        /// <summary>Azure client id</summary>
        [JsonProperty("clientid")]
        public string ClientId { get; set; }

        /// <summary>Azure client secret</summary>
        [JsonProperty("clientsecret")]
        public string ClientSecret { get; set; }

        /// <summary>Azure Tenant Id to be crawled</summary>
        [JsonProperty("tenantid")]
        public string TenantId { get; set; }

        /// <summary>Azure Subscription id to be crawled</summary>
        [JsonProperty("subscriptionid")]
        public string SubscriptionId { get; set; }

        /// <summary>Azure Resource group name to be crawled</summary>
        [JsonProperty("resourcegroup")]
        public string ResourceGroup { get; set; }
    }
}