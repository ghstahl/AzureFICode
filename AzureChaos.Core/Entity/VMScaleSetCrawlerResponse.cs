namespace AzureChaos.Core.Entity
{
    public class VmScaleSetCrawlerResponse : CrawlerResponse
    {
        public VmScaleSetCrawlerResponse()
        {
        }

        public VmScaleSetCrawlerResponse(string partitionKey, string rowKey)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
        }

        public bool HasVirtualMachines { get; set; }
        public int? AvailabilityZone { get; set; }
    }
}