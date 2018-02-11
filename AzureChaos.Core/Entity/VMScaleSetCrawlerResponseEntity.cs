namespace AzureChaos.Entity
{
    public class VMScaleSetCrawlerResponseEntity : CrawlerResponseEntity
    {
        public VMScaleSetCrawlerResponseEntity()
        {
        }

        public VMScaleSetCrawlerResponseEntity(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
        }

        public bool HasVirtualMachines { get; set; }
        public int? AvailabilityZone { get; set; }
    }
}