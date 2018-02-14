using Newtonsoft.Json;

namespace AzureChaos.Core.Models.Configs
{
    public class VirtualMachineChaosConfig
    {
        [JsonProperty("microsoft.chaos.VM.enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("microsoft.chaos.singleInstanceVm.percentageTermination")]
        public decimal PercentageTermination { get; set; }
    }
}