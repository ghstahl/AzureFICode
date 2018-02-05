using System.ComponentModel.DataAnnotations;

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
        /// <summary>Availability Set Key Id</summary>
        public string Key { get; set; }       

        /// <summary>Triggered Event </summary>
        public string Virtualmachines { get; set; }
        [Required]
        /// <summary>No: of Fault Domains in Availability Set</summary>
        public int FaultDomainCount { get; set; }
        [Required]
        /// <summary>No: of Update Domains in Availability Set</summary>
        public int UpdateDomainCount { get; set; }
    }
}