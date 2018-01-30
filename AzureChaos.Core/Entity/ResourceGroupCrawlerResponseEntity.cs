using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;
using AzureChaos.Entity;
using System.ComponentModel.DataAnnotations;
using AzureChaos.Enums;

namespace AzureChaos.Entity
{
    public class  ResourceGroupCrawlerResponseEntity:  TableEntity
    {
        public ResourceGroupCrawlerResponseEntity(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
        }
        [Required]
        /// <summary>Resource Group State</summary>
        public string ProvisionalState { get; set; }

        /// <summary>The Region Name</summary>
        public string RegionName { get; set; }

        [Required]
        /// <summary>Resource Group name </summary>
        public string ResourceGroupName { get; set; }
        [Required]
        /// <summary>Resource Group id </summary>
        public string Id { get; set; }

        [Required]
        /// <summary>DateTime whenRecord Entered into the Table</summary>
        public DateTime EntryInsertionTime { get; set; }

        /// <summary>Triggered Event </summary>
        public TriggeredEvent EventType { get; set; }

        /// <summary>Error message if anything occured on the time of execution.</summary>
        public string Error { get; set; }
    }
}
