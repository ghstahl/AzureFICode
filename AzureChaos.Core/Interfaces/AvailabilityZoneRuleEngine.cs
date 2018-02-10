using AzureChaos.Entity;
using AzureChaos.Helper;
using AzureChaos.Models;
using AzureChaos.Providers;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureChaos.Interfaces
{
    public class AvailabilityZoneRuleEngine : IRuleEngine
    {
        private AzureSettings azureSettings;
        //private CloudTable virtualMachineTable;
        //private IStorageAccountProvider storageProvider;

        public void CreateRule(AzureClient azureClient)
        {
            azureSettings = azureClient.azureSettings;
            Random random = new Random();
            IStorageAccountProvider storageAccountProvider = new StorageAccountProvider();
            var storageAccount = storageAccountProvider.CreateOrGetStorageAccount(azureClient);
            var possibleAvailabilityZoneRegionCombinations = GetAllPossibleAvailabilityZoneRegionCombination(storageAccountProvider, storageAccount);
            var recentlyExecutedAvailabilityZoneRegionCombination = GetRecentlyExecutedAvailabilityZoneRegionCombination();
            var avilabilityZoneRegionCombinations = possibleAvailabilityZoneRegionCombinations.Except(recentlyExecutedAvailabilityZoneRegionCombination).ToList();
            var randomAvailabilityZoneRegion = avilabilityZoneRegionCombinations[random.Next(0, avilabilityZoneRegionCombinations.Count - 1)];
            var componentsInRandomAvailabilityZoneRegion = randomAvailabilityZoneRegion.Split('!');
            var availabilityZone = int.Parse(componentsInRandomAvailabilityZoneRegion.Last());
            var region = componentsInRandomAvailabilityZoneRegion.First();
            InsertVirtualMachineAvailabilityZoneRegionResults(storageAccountProvider, storageAccount, region, availabilityZone);

        }

        private void InsertVirtualMachineAvailabilityZoneRegionResults(IStorageAccountProvider storageAccountProvider, CloudStorageAccount storageAccount, string region, int availbilityZone)
        {
            var virtualMachineQuery = TableQuery.CombineFilters((TableQuery.GenerateFilterConditionForInt("AvailabilityZone", QueryComparisons.Equal, availbilityZone)), TableOperators.And, (TableQuery.GenerateFilterCondition("RegionName", QueryComparisons.Equal, region)));
            //TableQuery.GenerateFilterConditionForInt("AvailabilityZone", QueryComparisons.GreaterThanOrEqual, 0);
            TableQuery<VirtualMachineCrawlerResponseEntity> virtualMachinesTableQuery = new TableQuery<VirtualMachineCrawlerResponseEntity>().Where(virtualMachineQuery);
            var crawledVirtualMachinesResults = storageAccountProvider.GetEntities<VirtualMachineCrawlerResponseEntity>(virtualMachinesTableQuery, storageAccount, azureSettings.VirtualMachineCrawlerTableName);
            if (crawledVirtualMachinesResults.Count() > 0)
            {
                var sessionId = Guid.NewGuid().ToString();
                TableBatchOperation scheduledRulesbatchOperation = VirtualMachineHelper.CreateScheduleEntityForAvailabilityZone(crawledVirtualMachinesResults.ToList(), azureSettings.Chaos.SchedulerFrequency);
                if (scheduledRulesbatchOperation.Count > 0)
                {
                    CloudTable table = storageAccountProvider.CreateOrGetTable(storageAccount, azureSettings.ScheduleTableName);
                    Extensions.Synchronize(() => table.ExecuteBatchAsync(scheduledRulesbatchOperation));
                }
            }
        }

        private List<string> GetRecentlyExecutedAvailabilityZoneRegionCombination()
        {
            List<string> recentlyExecutedAvailabilityZoneRegionCombination = new List<string>();

            return recentlyExecutedAvailabilityZoneRegionCombination;
        }
        private List<string> GetAllPossibleAvailabilityZoneRegionCombination(IStorageAccountProvider storageAccountProvider, CloudStorageAccount storageAccount)
        {
            //virtualMachineTable = storageAccountProvider.CreateOrGetTable(storageAccount, azureSettings.VirtualMachineCrawlerTableName);
            List<string> possibleAvailabilityZoneRegionCombination = new List<string>();
            Dictionary<string, int> possibleAvailabilityZoneRegionCombinationVMCount = new Dictionary<string, int>();
            var crawledAvailabilityZoneVMQuery =
                TableQuery.GenerateFilterConditionForInt("AvailabilityZone", QueryComparisons.GreaterThan, 0);
            TableQuery<VirtualMachineCrawlerResponseEntity> availabilityZoneTableQuery = new TableQuery<VirtualMachineCrawlerResponseEntity>().Where(crawledAvailabilityZoneVMQuery);
            var crawledVirtualMachinesResults = storageAccountProvider.GetEntities<VirtualMachineCrawlerResponseEntity>(availabilityZoneTableQuery, storageAccount, azureSettings.VirtualMachineCrawlerTableName);
            foreach (var eachCrawledVirtualMachinesResult in crawledVirtualMachinesResults)
            {
                var entryIntoPossibleAvailabilityZoneRegionCombinationVMCount = eachCrawledVirtualMachinesResult.RegionName + "!" + eachCrawledVirtualMachinesResult.AvailabilityZone.ToString();
                if (possibleAvailabilityZoneRegionCombinationVMCount.ContainsKey(entryIntoPossibleAvailabilityZoneRegionCombinationVMCount))
                {
                    possibleAvailabilityZoneRegionCombinationVMCount[entryIntoPossibleAvailabilityZoneRegionCombinationVMCount] += 1;
                }
                else
                {
                    possibleAvailabilityZoneRegionCombinationVMCount[entryIntoPossibleAvailabilityZoneRegionCombinationVMCount] = 1;
                }
            }
            return new List<string>(possibleAvailabilityZoneRegionCombinationVMCount.Keys);
        }
        public Task CreateRuleAsync(AzureClient azureClient)
        {
            throw new NotImplementedException();
        }

        public bool IsChaosEnabled(AzureSettings azureSettings)
        {
            throw new NotImplementedException();
        }
    }
}
