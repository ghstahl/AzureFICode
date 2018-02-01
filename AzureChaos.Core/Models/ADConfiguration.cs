using AzureChaos.Core.AzureClient;
using AzureChaos.Enums;
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
            GlobalConfig config = JsonConvert.DeserializeObject<GlobalConfig>(ExecuteGetWebRequest("https://cmonkeylogs.blob.core.windows.net/configs/globalconfig.json"));
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
        private static string ExecuteGetWebRequest(string webReqURL)
        {
            string result = string.Empty;
            try
            {
                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(webReqURL);
                httpWebRequest.Proxy = null;
                httpWebRequest.Method = WebRequestMethods.Http.Get;
                HttpWebResponse response = (HttpWebResponse)httpWebRequest.GetResponse();
                using (Stream stream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(stream, Encoding.UTF8, true);
                    result = reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }
    }
}
