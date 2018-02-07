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
    }
}
