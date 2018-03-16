﻿using Newtonsoft.Json;

namespace AzureChaos.Core.Models.Configs
{
    public class VirtualMachineChaosConfig
    {
        [JsonProperty("microsoft.faultinjection.VM.enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("microsoft.faultinjection.singleInstanceVm.percentageTermination")]
        public decimal PercentageTermination { get; set; }
    }
}