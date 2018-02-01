namespace AzureChaos.Entity
{
    public class VirtualMachineCrawlerResponseEntity : CrawlerResponseEntity
    {
        public VirtualMachineCrawlerResponseEntity(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
        }
        
        /// <summary>Available Set Id. i.e. Vm belongs to which avaiable set if any.</summary>
        public string AvailableSetId { get; set; }

        /// <summary>The virtual machine group name i.e. the virtual machine belongs to which resource type ex. is it from Available Set, Scale Set  or Load balancers etc...</summary>
        public string VirtualMachineGroup { get; set; }
    }
}