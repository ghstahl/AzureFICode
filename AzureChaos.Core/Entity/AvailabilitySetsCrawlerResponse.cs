using System.ComponentModel.DataAnnotations;

namespace AzureChaos.Core.Entity
{
    public class AvailabilitySetsCrawlerResponseEntity : CrawlerResponse
    {
        public AvailabilitySetsCrawlerResponseEntity()
        { }

        public AvailabilitySetsCrawlerResponseEntity(string partitionKey, string rowKey)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
        }

        [Required] public string Key { get; set; }

        /// <summary>Triggered Event </summary>
        public string Virtualmachines { get; set; }

        [Required] public int FaultDomainCount { get; set; }

        [Required] public int UpdateDomainCount { get; set; }
    }
}