﻿using System;
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
        public string SchedulerSessionId { get; set; }
        public bool Rollbacked { get; set; }
        public DateTime? ScheduledExecutionTime { get; set; }
        public string ExecutionStatus { get; set; }
        public string ChaosAction { get; set; }
        public string CombinationKey { get; set; }
        public DateTime? ExecutionStartTime { get; set; }

        /// <summary>Event completed date time.</summary>
        public DateTime? EventCompletedTime { get; set; }

        /// <summary>Initial State of the resource</summary>
        public string InitialState { get; set; }

        /// <summary>Final State of the resource</summary>
        public string FinalState { get; set; }

        /// <summary>Error message if anything occured on the time of execution.</summary>
        public string Warning { get; set; }

        /// <summary>Error message if anything occured on the time of execution.</summary>
        public string Error { get; set; }
    }
}