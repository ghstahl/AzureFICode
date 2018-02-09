using Newtonsoft.Json;

namespace AzureChaos.Models
{
    public class VirtualMachineChaosConfig
    {
        [JsonProperty("microsoft.chaos.VM.enabled")]
        public string Enabled { get; set; }

        [JsonProperty("microsoft.chaos.singleInstanceVm.percentageTermination")]
        public decimal percentageTermination { get; set; }
    }
}