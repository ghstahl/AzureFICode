using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AzureChaos.Entity
{
    public class AvailabilitySetsCrawlerResponseEntity : CrawlerResponseEntity
    {
        public AvailabilitySetsCrawlerResponseEntity(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
        }

        [Required]
        /// <summary>Resource Group State</summary>
        public string Key { get; set; }

        /// <summary>Triggered Event </summary>
        public string Virtualmachines { get; set; }

        /// <summary>Fault domain count for the availability set.</summary>
        public int FaultDomainCount { get; set; }

        /// <summary>Update domain count for the availability set.</summary>
        public int UpdateDomainCount { get; set; }
    }
}