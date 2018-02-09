using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureChaos.Entity
{
    public class ScheduledRulesEntity : TableEntity
    {
        public ScheduledRulesEntity()
        { }

        public ScheduledRulesEntity(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
        }

        public string triggerData { get; set; }

        public string executorEndPoint { get; set; }
        public string schedulerSessionId { get; set; }
        public bool isRollBack { get; set; }
        public DateTime scheduledExecutionTime { get; set; }
    }
}
