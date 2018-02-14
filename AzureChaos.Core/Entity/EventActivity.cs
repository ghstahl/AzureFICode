using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.ComponentModel.DataAnnotations;

namespace AzureChaos.Core.Entity
{
    public class EventActivity : TableEntity
    {
        public EventActivity()
        {
        }

        public EventActivity(string partitionKey, string rowKey = "")
        {
            PartitionKey = partitionKey;
            RowKey = string.IsNullOrWhiteSpace(rowKey) ? Guid.NewGuid().ToString() : rowKey;
        }

        [Required] public string ResourceType { get; set; }

        /// <summary>The Resource name.</summary>
        public string Resource { get; set; }

        /// <summary>The Resource name.</summary>
        public string Id { get; set; }

        [Required] public string ResourceGroup { get; set; }

        [Required] public DateTime EntryDate { get; set; }

        [Required] public DateTime EventStateDate { get; set; }

        /// <summary>Event completed date time.</summary>
        public DateTime? EventCompletedDate { get; set; }

        /// <summary>Status Of the operation.</summary>
        public string Status { get; set; }

        /// <summary>Initial State of the resource</summary>
        public string InitialState { get; set; }

        /// <summary>Final State of the resource</summary>
        public string FinalState { get; set; }

        /// <summary>Chaos type on the resource</summary>
        public string EventType { get; set; }

        /// <summary>Error message if anything occured on the time of execution.</summary>
        public string Warning { get; set; }

        /// <summary>Error message if anything occured on the time of execution.</summary>
        public string Error { get; set; }
    }
}