using System;
using System.Collections.Generic;
using System.Text;

namespace AzureChaos.Core.AzureClient
{
    class GlobalConfig
    {
        public string subscription { get; set; }
        public string client { get; set; }
        public string key { get; set; }
        public string tenant { get; set; }
        public string RulesTriggerTimeStart { get; set; }
        public string RulesTriggerTimeEnd { get; set; }
        public int RulesBufferTime { get; set; }
        public int CrawlerFrquency { get; set; }
        public string SchedulerFrequency { get; set; }
        public string Resources { get; set; }
        public string CommonLogsStorageAccount { get; set; }
        public string ActivityLogTableName { get; set; }
        public string RGCrawlerTableName { get; set; }
        public string VMCrawlerTableName { get; set; }
        public string DefaultRegion { get; set; }
        public string DefaultResourceGroup { get; set; }
    }
}
