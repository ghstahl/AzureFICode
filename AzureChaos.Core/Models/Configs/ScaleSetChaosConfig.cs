using Newtonsoft.Json;

namespace AzureChaos.Core.Models.Configs
{
    public class ScaleSetChaosConfig
    {
        [JsonProperty("microsoft.chaos.SS.enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("microsoft.chaos.SS.percentageTermination")]
        public decimal PercentageTermination { get; set; }
    }
}