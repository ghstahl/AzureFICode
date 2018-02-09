using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace AzureChaos.Models
{
    public class ChaosConfig
    {
        [JsonProperty("microsoft.chaos.enabled")]
        public bool ChaosEnabled { get; set; }

        [JsonProperty("microsoft.chaos.leashed")]
        public bool Leashed { get; set; }

        #region Trigger settings -currently commented

        /*[JsonProperty("microsoft.chaos.startTime")]
        public string StartTime { get; set; }

        [JsonProperty("microsoft.chaos.endTime")]
        public string EndTime { get; set; }

        [JsonProperty("microsoft.chaos.crawlerRunFrequencyInMins")]
        public string CrawlerRunFrequency { get; set; }

        [JsonProperty("microsoft.chaos.SchedulerRunFrequencyInMins")]
        public string SchedulerRunFrequency { get; set; }*/

        #endregion Trigger settings -currently commented

        [JsonProperty("microsoft.chaos.notification.global.enabled")]
        public bool NotificationEnabled { get; set; }

        [JsonProperty("microsoft.chaos.notification.sourceEmail")]
        public string SourceEmail { get; set; }

        [JsonProperty("microsoft.chaos.notification.global.receiverEmail")]
        public string ReceiverEmail { get; set; }

        [JsonProperty("microsoft.chaos.blackListedResources")]
        public List<string> BlackListedResources { get; set; }

        [JsonProperty("microsoft.chaos.blackListedResourceGroups")]
        public string BloackListedResourceGroups { get; set; }

        [JsonProperty("microsoft.chaos.resourceGroups")]
        public string ResourceGroups { get; set; }

        [JsonProperty("microsoft.chaos.AS")]
        public AvailabilitySetChaosConfig AvailabilitySetChaos { get; set; }

        [JsonProperty("microsoft.chaos.SS")]
        public ScaleSetChaosConfig ScaleSetChaos { get; set; }

        [JsonProperty("microsoft.chaos.VM")]
        public VirtualMachineChaosConfig VirtualMachineChaos { get; set; }

        [JsonProperty("microsoft.chaos.AZ")]
        public AvailabilityZoneChaosConfig AvailabilityZoneChaos { get; set; }
    }
}
