using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace AzureChaos.Entity
{
    public class ScheduleEntity : TableEntity
    {
        public ScheduleEntity()
        {
        }

        public ScheduleEntity(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
        }

        public string Id { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EntryInsertionTime { get; set; }

        public string Action { get; set; }

        public string ResourceName { get; set; }

        public string Error { get; set; }
    }
}
