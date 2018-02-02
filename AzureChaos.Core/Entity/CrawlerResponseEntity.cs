using AzureChaos.Enums;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.ComponentModel.DataAnnotations;

namespace AzureChaos.Entity
{
    public class CrawlerResponseEntity : TableEntity
    {
        /// <summary>The Region Name</summary>
        public string RegionName { get; set; }

        [Required]
        /// <summary>Resource Group name </summary>
        public string ResourceGroupName { get; set; }

        [Required]
        /// <summary>DateTime whenRecord Entered into the Table</summary>
        public DateTime EntryInsertionTime { get; set; }

        [Required]
        /// <summary>Resource Type name</summary>
        public string ResourceType { get; set; }

        /// <summary>The Resource Name.</summary>
        public string ResourceName { get; set; }

        /// <summary>Resource Id </summary>
        public string Id { get; set; }

        /// <summary>Triggered Event </summary>
        public TriggeredEvent EventType { get; set; }

        /// <summary>Error message if anything occured on the time of execution.</summary>
        public string Error { get; set; }
    }
}
