using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.ComponentModel.DataAnnotations;

namespace AzureChaos.Entity
{
    public class EventActivityEntity : TableEntity
    {
        public EventActivityEntity()
        {
        }

        public EventActivityEntity(string partitionKey, string rowKey = "")
        {
            this.PartitionKey = partitionKey;
            this.RowKey = string.IsNullOrWhiteSpace(rowKey) ? Guid.NewGuid().ToString() : rowKey;
        }

        [Required]
        /// <summary>The Resource type. ex. Load balancers, Stand alone vm's, Network interface, Virtual Network etc...</summary>
        public string ResourceType { get; set; }

        /// <summary>The Resource name.</summary>
        public string Resource { get; set; }

        /// <summary>The Resource name.</summary>
        public string Id { get; set; }

        [Required]
        /// <summary>Resource Group name </summary>
        public string ResourceGroup { get; set; }

        [Required]
        /// <summary>Table entry date</summary>
        public DateTime EntryDate { get; set; }

        [Required]
        /// <summary>Event start date time.</summary>
        public DateTime EventStateDate { get; set; }

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