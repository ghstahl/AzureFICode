using AzureChaos.Core.Entity;
using AzureChaos.Core.Enums;
using AzureChaos.Core.Helper;
using AzureChaos.Core.Models;
using AzureChaos.Core.Providers;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureChaos.Core.Models.Configs;

namespace AzureChaos.Core.Interfaces
{
    public class AvailabilityZoneRuleEngine : IRuleEngine
    {
        private AzureSettings _azureSettings;

        public void CreateRule(AzureClient azureClient, TraceWriter log)
        {
            log.Info("AvailabilityZone RuleEngine: Started the creating rules for the scale set.");
            try
            {
                _azureSettings = azureClient.AzureSettings;
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
                var random = new Random();
                var randomAvailabilityZoneRegion = avilabilityZoneRegionCombinationsList[random.Next(0, avilabilityZoneRegionCombinationsList.Count - 1)];
                var componentsInRandomAvailabilityZoneRegion = randomAvailabilityZoneRegion.Split('!');
                var availabilityZone = int.Parse(componentsInRandomAvailabilityZoneRegion.Last());
                var region = componentsInRandomAvailabilityZoneRegion.First();
                InsertVirtualMachineAvailabilityZoneRegionResults(storageAccountProvider, storageAccount, region, availabilityZone);
            }
            catch (Exception ex)
            {
                log.Error("AvailabilityZone RuleEngine: thrown exception", ex);
            }
        }

        private void InsertVirtualMachineAvailabilityZoneRegionResults(IStorageAccountProvider storageAccountProvider, CloudStorageAccount storageAccount, string region, int availbilityZone)
        {
            var virtualMachineQuery = TableQuery.CombineFilters((TableQuery.GenerateFilterConditionForInt("AvailabilityZone", QueryComparisons.Equal, availbilityZone)), TableOperators.And, (TableQuery.GenerateFilterCondition("RegionName", QueryComparisons.Equal, region)));
            //TableQuery.GenerateFilterConditionForInt("AvailabilityZone", QueryComparisons.GreaterThanOrEqual, 0);
            var virtualMachinesTableQuery = new TableQuery<VirtualMachineCrawlerResponse>().Where(virtualMachineQuery);
            var crawledVirtualMachinesResults = storageAccountProvider.GetEntities(
                virtualMachinesTableQuery,
                storageAccount,
                _azureSettings.VirtualMachineCrawlerTableName);

            var virtualMachinesResults = crawledVirtualMachinesResults.ToList();
            if (!virtualMachinesResults.Any()) return;
            var scheduledRulesbatchOperation = VirtualMachineHelper.CreateScheduleEntityForAvailabilityZone(virtualMachinesResults.ToList(), _azureSettings.Chaos.SchedulerFrequency);
            if (scheduledRulesbatchOperation.Count <= 0) return;
            var table = storageAccountProvider.CreateOrGetTable(storageAccount, _azureSettings.ScheduledRulesTable);
            Extensions.Synchronize(() => table.ExecuteBatchAsync(scheduledRulesbatchOperation));
        }

        private IEnumerable<string> GetRecentlyExecutedAvailabilityZoneRegionCombination(IStorageAccountProvider storageAccountProvider, CloudStorageAccount storageAccount)
        {
            var recentlyExecutedAvailabilityZoneRegionCombination = new List<string>();
            var possibleAvailabilityZoneRegionCombinationVmCount = new Dictionary<string, int>();
            var meanTimeQuery = TableQuery.GenerateFilterConditionForDate("scheduledExecutionTime",
                QueryComparisons.GreaterThanOrEqual,
                DateTimeOffset.UtcNow.AddMinutes(-_azureSettings.Chaos.SchedulerFrequency));

            var recentlyExecutedAvailabilityZoneRegionCombinationQuery = TableQuery.GenerateFilterCondition(
                "PartitionKey",
                QueryComparisons.Equal,
                VirtualMachineGroup.AvailabilityZones.ToString());

            var recentlyExecutedFinalAvailabilityZoneRegionQuery = TableQuery.CombineFilters(meanTimeQuery,
                TableOperators.And,
                recentlyExecutedAvailabilityZoneRegionCombinationQuery);

            var scheduledQuery = new TableQuery<ScheduledRules>().Where(recentlyExecutedFinalAvailabilityZoneRegionQuery);
            var executedAvilabilityZoneCombinationResults = storageAccountProvider.GetEntities(scheduledQuery, storageAccount, _azureSettings.ScheduledRulesTable);
            if (executedAvilabilityZoneCombinationResults == null)
                return recentlyExecutedAvailabilityZoneRegionCombination;

            foreach (var eachExecutedAvilabilityZoneCombinationResults in executedAvilabilityZoneCombinationResults)
            {
                if (!eachExecutedAvilabilityZoneCombinationResults.CombinationKey.Contains("!")) continue;

                if (possibleAvailabilityZoneRegionCombinationVmCount.ContainsKey(eachExecutedAvilabilityZoneCombinationResults.CombinationKey))
                {
                    possibleAvailabilityZoneRegionCombinationVmCount[eachExecutedAvilabilityZoneCombinationResults.CombinationKey] += 1;
                }
                else
                {
                    possibleAvailabilityZoneRegionCombinationVmCount[eachExecutedAvilabilityZoneCombinationResults.CombinationKey] = 1;
                }

            }
            recentlyExecutedAvailabilityZoneRegionCombination = new List<string>(possibleAvailabilityZoneRegionCombinationVmCount.Keys);
            return recentlyExecutedAvailabilityZoneRegionCombination;
        }
        private List<string> GetAllPossibleAvailabilityZoneRegionCombination(IStorageAccountProvider storageAccountProvider, CloudStorageAccount storageAccount)
        {
            //virtualMachineTable = storageAccountProvider.CreateOrGetTable(storageAccount, azureSettings.VirtualMachineCrawlerTableName);
            var possibleAvailabilityZoneRegionCombinationVmCount = new Dictionary<string, int>();
            var crawledAvailabilityZoneVmQuery =
                TableQuery.GenerateFilterConditionForInt("AvailabilityZone", QueryComparisons.GreaterThan, 0);

            var availabilityZoneTableQuery = new TableQuery<VirtualMachineCrawlerResponse>().Where(crawledAvailabilityZoneVmQuery);
            var crawledVirtualMachinesResults = storageAccountProvider.GetEntities(availabilityZoneTableQuery, storageAccount, _azureSettings.VirtualMachineCrawlerTableName);
            foreach (var eachCrawledVirtualMachinesResult in crawledVirtualMachinesResults)
            {
                var entryIntoPossibleAvailabilityZoneRegionCombinationVmCount = eachCrawledVirtualMachinesResult.RegionName + "!" + eachCrawledVirtualMachinesResult.AvailabilityZone.ToString();
                if (possibleAvailabilityZoneRegionCombinationVmCount.ContainsKey(entryIntoPossibleAvailabilityZoneRegionCombinationVmCount))
                {
                    possibleAvailabilityZoneRegionCombinationVmCount[entryIntoPossibleAvailabilityZoneRegionCombinationVmCount] += 1;
                }
                else
                {
                    possibleAvailabilityZoneRegionCombinationVmCount[entryIntoPossibleAvailabilityZoneRegionCombinationVmCount] = 1;
                }
            }

            return new List<string>(possibleAvailabilityZoneRegionCombinationVmCount.Keys);
        }
        public Task CreateRuleAsync(AzureClient azureClient)
        {
            throw new NotImplementedException();
        }
    }
}
