using Newtonsoft.Json;

namespace AzureChaos.Models
{
    public class AvailabilitySetChaosConfig
    {
        [JsonProperty("microsoft.chaos.AS.enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("microsoft.chaos.AS.faultDomain.enabled")]
        public bool FaultDomainEnabled { get; set; }

        [JsonProperty("microsoft.chaos.AS.updateDomain.enabled")]
        public bool UpdateDomainEnabled { get; set; }
    }
}
