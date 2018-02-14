using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureChaos.Core.Entity
{
    public class ScheduledRules : TableEntity
    {
        public ScheduledRules()
        { }

        public ScheduledRules(string partitionKey, string rowKey)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
        }

        public string TriggerData { get; set; }

        public string ExecutorEndPoint { get; set; }
        public string SchedulerSessionId { get; set; }
        public bool IsRollBack { get; set; }
        public DateTime ScheduledExecutionTime { get; set; }
        public string Status { get; set; }
        public string ChaosAction { get; set; }
        public string CombinationKey { get; set; }
    }
}
