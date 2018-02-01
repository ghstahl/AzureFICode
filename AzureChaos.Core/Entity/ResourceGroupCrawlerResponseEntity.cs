using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;
using AzureChaos.Entity;
using System.ComponentModel.DataAnnotations;
using AzureChaos.Enums;

namespace AzureChaos.Entity
{
    public class ResourceGroupCrawlerResponseEntity : CrawlerResponseEntity
    {
        public ResourceGroupCrawlerResponseEntity() { }
        public ResourceGroupCrawlerResponseEntity(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
        }

        [Required]
        /// <summary>Resource Group State</summary>
        public string ProvisionalState { get; set; }

        [Required]
        /// <summary>Resource Group name </summary>
        public string ResourceGroupId { get; set; }
    }
}
