using Newtonsoft.Json;
using System.Collections.Generic;

namespace AzureChaos.Models
{
    public class AvailabilityZoneChaosConfig
    {
        [JsonProperty("microsoft.chaos.AZ.enabled")]
        public string Enabled { get; set; }

        [JsonProperty("microsoft.chaos.AZ.regions")]
        public List<string> Regions { get; set; }
    }
}
