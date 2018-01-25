using AzureChaos.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

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

        /// <summary>Defines resource types which want to exclude from the chaos. its defined by comma separated.</summary>
        [JsonProperty("excludedresourcetype")]
        public string ExcludedResourceType { get; set; }

        [JsonIgnore]
        public List<ResourceType> ExcludeResourceTypeList
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ExcludedResourceType))
                {
                    return null;
                }

                var resourceTypeList = ExcludedResourceType.Split(',');
                if (!resourceTypeList.Any())
                {
                    return null;
                }

                var resources = resourceTypeList.Select(x =>
                {
                    ResourceType type;
                    if (Enum.TryParse(x, out type))
                    {
                        return type;
                    }

                    return type;
                }).Where(x => x != ResourceType.Unknown);

                return resources.Any() ? resources.ToList() : null;
            }
        }
    }
}
