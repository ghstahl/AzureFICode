using AzureChaos.Enums;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.ComponentModel.DataAnnotations;

namespace AzureChaos.Entity
{
    /// <summary>The chaos sceduled table entity.</summary>
    public class ChaosScheduleTableEntity : TableEntity
    {
        public ChaosScheduleTableEntity()
        {
        }
        public ChaosScheduleTableEntity(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
        }

        [Required]
        /// <summary>The Resource type. ex. Load balancers, Stand alone vm's, Network interface, Virtual Network etc...</summary>
        public string ResourceType { get; set; }

        /// <summary>The Resource name.</summary>
        public string Resource { get; set; }

        [Required]
        /// <summary>Resource Group name </summary>
        public string ResourceGroup { get; set; }

        [Required]
        /// <summary>Table entry date</summary>
        public DateTime EntryDate { get; set; }

        [Required]
        /// <summary>Chaos Start Date, when chaos should get started.</summary>
        public DateTime ChaosStartDate { get; set; }

        /// <summary>Chaos End Date, when job ended.</summary>
        public DateTime? ChaosEndDate { get; set; }

        /// <summary>Chaos Job state</summary>
        public State State { get; set; }

        /// <summary>Status of the chaos job, whether its success or failure.</summary>
        public bool Status { get; set; }

        [Required]
        /// <summary>Command to trigger the chaos job</summary>
        public string TriggerCommand { get; set; }
    }
}