using AzureChaos.ReguestHelpers;
using Newtonsoft.Json;

namespace AzureChaos.Models
{
    public class AzureSettings
    {
        [JsonProperty("ClientConfig")]
        public ClientConfig Client { get; set; }

        [JsonProperty("ChaosConfig")]
        public ChaosConfig Chaos { get; set; }

        /// <summary> Do we keep these storage account table information in the Config files OR will keep as constant name?</summary>
        [JsonProperty("microsoft.chaos.client.table.resourceGroupCrawler")]
        public string ResourceGroupCrawlerTableName { get; set; }

        [JsonProperty("microsoft.chaos.client.table.virtualMachineCrawler")]
        public string VirtualMachineCrawlerTableName { get; set; }

        [JsonProperty("microsoft.chaos.client.table.availabilitySetCrawler")]
        public string AvailabilitySetCrawlerTableName { get; set; }

        [JsonProperty("microsoft.chaos.client.table.scaleSetCrawler")]
        public string ScaleSetCrawlerTableName { get; set; }

        [JsonProperty("microsoft.chaos.client.table.availabilityZoneCrawler")]
        public string AvailabilityZoneCrawlerTableName { get; set; }

        [JsonProperty("microsoft.chaos.client.table.scheduleTable")]
        public string ScheduleTableName { get; set; }

        [JsonProperty("microsoft.chaos.client.table.activityLog")]
        public string ActivityLogTable { get; set; }

        [JsonProperty("microsoft.chaos.client.table.scheduledRules")]
        public string ScheduledRulesTable { get; set; }

        [JsonProperty("storageaccountname")]
        public string StorageAccountName { get; set; }

        [JsonProperty("microsoft.chaos.AS.faultDomain.enabled")]
        public bool EnableAvailabilitySet { get; set; }
    }
}
