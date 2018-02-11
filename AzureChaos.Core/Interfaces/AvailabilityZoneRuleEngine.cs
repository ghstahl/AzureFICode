using AzureChaos.Entity;
using AzureChaos.Helper;
using AzureChaos.Models;
using AzureChaos.Providers;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureChaos.Enums;

namespace AzureChaos.Interfaces
{
    public class AvailabilityZoneRuleEngine : IRuleEngine
    {
        private AzureSettings azureSettings;

        public void CreateRule(AzureClient azureClient, TraceWriter log)
        {
            log.Info("AvailabilityZone RuleEngine: Started the creating rules for the scale set.");
            try
            {
                azureSettings = azureClient.azureSettings;
                IStorageAccountProvider storageAccountProvider = new StorageAccountProvider();
                var storageAccount = storageAccountProvider.CreateOrGetStorageAccount(azureClient);
                var possibleAvailabilityZoneRegionCombinations = GetAllPossibleAvailabilityZoneRegionCombination(storageAccountProvider, storageAccount);
                if (possibleAvailabilityZoneRegionCombinations == null)
                {
                    log.Info("AvailabilityZone RuleEngine: Not found any possible Avilability zones");
                    return;
                }

                var recentlyExecutedAvailabilityZoneRegionCombination = GetRecentlyExecutedAvailabilityZoneRegionCombination(storageAccountProvider, storageAccount);
                var avilabilityZoneRegionCombinations = possibleAvailabilityZoneRegionCombinations.Except(recentlyExecutedAvailabilityZoneRegionCombination);
                if (avilabilityZoneRegionCombinations == null)
                {
                    log.Info("AvailabilityZone RuleEngine: Not found any Avilability zones after excluding the recent availabity zone");
                    return;
                }

                var avilabilityZoneRegionCombinationsList = avilabilityZoneRegionCombinations.ToList();
                Random random = new Random();
                var randomAvailabilityZoneRegion = avilabilityZoneRegionCombinationsList[random.Next(0, avilabilityZoneRegionCombinationsList.Count - 1)];
                var componentsInRandomAvailabilityZoneRegion = randomAvailabilityZoneRegion.Split('!');
                var availabilityZone = int.Parse(componentsInRandomAvailabilityZoneRegion.Last());
                var region = componentsInRandomAvailabilityZoneRegion.First();
                InsertVirtualMachineAvailabilityZoneRegionResults(storageAccountProvider, storageAccount, region, availabilityZone);
            }
            catch (Exception ex)
            {
                log.Error("AvailabilityZone RuleEngine: thrown exception", ex);
                return;
            }
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
                    CloudTable table = storageAccountProvider.CreateOrGetTable(storageAccount, azureSettings.ScheduledRulesTable);
                    Extensions.Synchronize(() => table.ExecuteBatchAsync(scheduledRulesbatchOperation));
                }
            }
        }

        private List<string> GetRecentlyExecutedAvailabilityZoneRegionCombination(IStorageAccountProvider storageAccountProvider, CloudStorageAccount storageAccount)
        {
            List<string> recentlyExecutedAvailabilityZoneRegionCombination = new List<string>();
            Dictionary<string, int> possibleAvailabilityZoneRegionCombinationVMCount = new Dictionary<string, int>();
            var meanTimeQuery = TableQuery.GenerateFilterConditionForDate("scheduledExecutionTime", QueryComparisons.GreaterThanOrEqual, DateTimeOffset.UtcNow.AddMinutes(-120));
            var recentlyExecutedAvailabilityZoneRegionCombinationQuery = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, VirtualMachineGroup.AvailabilityZones.ToString());
            var recentlyExecutedFinalAvailabilityZoneRegionQuery = TableQuery.CombineFilters(meanTimeQuery, TableOperators.And, recentlyExecutedAvailabilityZoneRegionCombinationQuery);
            TableQuery<ScheduledRulesEntity> scheduledQuery = new TableQuery<ScheduledRulesEntity>().Where(recentlyExecutedFinalAvailabilityZoneRegionQuery);
            var executedAvilabilityZoneCombinationResults = storageAccountProvider.GetEntities<ScheduledRulesEntity>(scheduledQuery, storageAccount, azureSettings.ScheduledRulesTable);
            if (executedAvilabilityZoneCombinationResults != null)
            {
                foreach (var eachExecutedAvilabilityZoneCombinationResults in executedAvilabilityZoneCombinationResults)
                {
                    if (eachExecutedAvilabilityZoneCombinationResults.combinationKey.Contains("!"))
                    {
                        if (possibleAvailabilityZoneRegionCombinationVMCount.ContainsKey(eachExecutedAvilabilityZoneCombinationResults.combinationKey))
                        {
                            possibleAvailabilityZoneRegionCombinationVMCount[eachExecutedAvilabilityZoneCombinationResults.combinationKey] += 1;
                        }
                        else
                        {
                            possibleAvailabilityZoneRegionCombinationVMCount[eachExecutedAvilabilityZoneCombinationResults.combinationKey] = 1;
                        }
                    }

                }
                recentlyExecutedAvailabilityZoneRegionCombination = new List<string>(possibleAvailabilityZoneRegionCombinationVMCount.Keys);
            }
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
    }
}
