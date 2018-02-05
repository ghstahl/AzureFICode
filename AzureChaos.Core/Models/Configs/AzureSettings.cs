using Newtonsoft.Json;

namespace AzureChaos.Models
{
    public class AzureSettings
    {
        [JsonProperty("subscriptionid")]
        public string SubscriptionId { get; set; }

        [JsonProperty("clientid")]
        public string ClientId { get; set; }

        [JsonProperty("clientsecret")]
        public string ClientSecret { get; set; }

        [JsonProperty("tenantid")]
        public string TenantId { get; set; }

        [JsonProperty("region")]
        public string Region { get; set; }

        [JsonProperty("resourcegroup")]
        public string ResourceGroup { get; set; }

        [JsonProperty("resourcegroupcrawlertablename")]
        public string ResourceGroupCrawlerTableName { get; set; }

        [JsonProperty("virtualmachinecrawlertablename")]
        public string VirtualMachineCrawlerTableName { get; set; }

        [JsonProperty("availabilitysetcrawlertablename")]
        public string AvailabilitySetCrawlerTableName { get; set; }

        [JsonProperty("scalesetcrawlertablename")]
        public string ScaleSetCrawlerTableName { get; set; }

        [JsonProperty("availabilityzonecrawlertablename")]
        public string AvailabilityZoneCrawlerTableName { get; set; }

        [JsonProperty("activitylogtable")]
        public string ActivityLogTable { get; set; }

        [JsonProperty("storageaccountname")]
        public string StorageAccountName { get; set; }
    }
}
