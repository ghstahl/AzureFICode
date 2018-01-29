using AzureChaos.Enums;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.ComponentModel.DataAnnotations;

namespace AzureChaos.Entity
{
    public class EventActivity : TableEntity
    {
        public EventActivity(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
        }

        [Required]
        /// <summary>The Resource type. ex. Load balancers, Stand alone vm's, Network interface, Virtual Network etc...</summary>
        public ResourceType ResourceType { get; set; }

        /// <summary>The Resource name.</summary>
        public string Resource { get; set; }

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

        /// <summary>Initial State of the resource</summary>
        public State InitialState { get; set; }

        /// <summary>Final State of the resource</summary>
        public State FinalState { get; set; }

        /// <summary>Chaos type on the resource</summary>
        public ActionType EventType { get; set; }

        /// <summary>Error message if anything occured on the time of execution.</summary>
        public string Error { get; set; }
    }
}
