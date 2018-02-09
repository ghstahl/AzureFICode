using Newtonsoft.Json;

namespace AzureChaos.Models
{
    public class ScaleSetChaosConfig
    {
        [JsonProperty("microsoft.chaos.SS.enabled")]
        public string Enabled { get; set; }

        [JsonProperty("microsoft.chaos.SS.percentageTermination")]
        public decimal percentageTermination { get; set; }
    }
}