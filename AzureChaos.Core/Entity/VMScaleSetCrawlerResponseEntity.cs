using AzureChaos.Entity;

namespace AzureChaos.Entity
{
    public class VMScaleSetCrawlerResponseEntity : CrawlerResponseEntity
    {
        public VMScaleSetCrawlerResponseEntity(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
        }

        public string ResourceName { get; set; }
    }
}
