using AzureChaos.Entity;
using AzureChaos.Enums;
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

namespace AzureChaos.Interfaces
{
    public class AvailabilitySetRuleEngine : IRuleEngine
    {
        private AzureSettings azureSettings;

        public void CreateRule(AzureClient azureClient, TraceWriter log)
        {
            try
            {
                log.Info("Availability RuleEngine: Started the creating rules for the availability set.");
                azureSettings = azureClient.azureSettings;
                Random random = new Random();
                IStorageAccountProvider storageAccountProvider = new StorageAccountProvider();
                var storageAccount = storageAccountProvider.CreateOrGetStorageAccount(azureClient);
                //1) OpenSearch with Vm Count > 0
                var possibleAvailabilitySets = GetPossibleAvailabilitySets(storageAccountProvider, storageAccount);
                if (possibleAvailabilitySets == null)
                {
                    log.Info("Availability RuleEngine: Not found any Avilability sets with virtual machines");
                    return;
                }

                var recentlyExcludedAvailabilitySetDomainCombination = GetRecentlyExecutedAvailabilitySetDomainCombination(storageAccountProvider, storageAccount);
                var availableSetDomainOptions = possibleAvailabilitySets.Except(recentlyExcludedAvailabilitySetDomainCombination);
                if (availableSetDomainOptions == null)
                {
                    log.Info("Availability RuleEngine: Not found any Avilability sets after excluding the recent availabity sets");
                    return;
                }

                var availableSetDomainOptionsList = availableSetDomainOptions.ToList();
                var randomAvailabilitySetDomainCombination = availableSetDomainOptionsList[random.Next(0, availableSetDomainOptionsList.Count - 1)];
                var componentsInAvailabilitySetDomainCombination = randomAvailabilitySetDomainCombination.Split('@');
                var domainNumber = int.Parse(componentsInAvailabilitySetDomainCombination.Last());
                var availabilitySetId = componentsInAvailabilitySetDomainCombination.First();
                InsertVirtualMachineAvailabilitySetDomainResults(storageAccountProvider, storageAccount, availabilitySetId, domainNumber);
                log.Error("Availability RuleEngine: Completed creating rule engine");
            }
            catch (Exception ex)
            {
                log.Error("Availability RuleEngine: Exception thrown. ", ex);
                return;
            }
        }

        private void InsertVirtualMachineAvailabilitySetDomainResults(IStorageAccountProvider storageAccountProvider, CloudStorageAccount storageAccount, string availabilitySetId, int domainNumber)
        {
            var virtualMachineQuery = string.Empty;
            if (azureSettings.Chaos.AvailabilitySetChaos.FaultDomainEnabled)
            {
                virtualMachineQuery = TableQuery.CombineFilters((TableQuery.GenerateFilterCondition("AvailableSetId", QueryComparisons.Equal, availabilitySetId)), TableOperators.And, (TableQuery.GenerateFilterConditionForInt("FaultDomain", QueryComparisons.Equal, domainNumber)));
            }
            else
            {
                virtualMachineQuery = TableQuery.CombineFilters((TableQuery.GenerateFilterCondition("AvailableSetId", QueryComparisons.Equal, availabilitySetId)), TableOperators.And, (TableQuery.GenerateFilterConditionForInt("UpdateDomain", QueryComparisons.Equal, domainNumber)));
            }
            //TableQuery.GenerateFilterConditionForInt("AvailabilityZone", QueryComparisons.GreaterThanOrEqual, 0);
            TableQuery<VirtualMachineCrawlerResponseEntity> virtualMachinesTableQuery = new TableQuery<VirtualMachineCrawlerResponseEntity>().Where(virtualMachineQuery);
            var crawledVirtualMachinesResults = storageAccountProvider.GetEntities<VirtualMachineCrawlerResponseEntity>(virtualMachinesTableQuery, storageAccount, azureSettings.VirtualMachineCrawlerTableName);
            if (crawledVirtualMachinesResults.Count() > 0)
            {
                var sessionId = Guid.NewGuid().ToString();
                bool domainFlage = true;
                if (azureSettings.Chaos.AvailabilitySetChaos.UpdateDomainEnabled)
                {
                    domainFlage = false;
                }
                TableBatchOperation scheduledRulesbatchOperation = VirtualMachineHelper.CreateScheduleEntityForAvailabilitySet(crawledVirtualMachinesResults.ToList(), azureSettings.Chaos.SchedulerFrequency, domainFlage);
                if (scheduledRulesbatchOperation.Count > 0)
                {
                    CloudTable table = storageAccountProvider.CreateOrGetTable(storageAccount, azureSettings.ScheduledRulesTable);
                    Extensions.Synchronize(() => table.ExecuteBatchAsync(scheduledRulesbatchOperation));
                }
            }
        }

        private List<string> GetRecentlyExecutedAvailabilitySetDomainCombination(IStorageAccountProvider storageAccountProvider, CloudStorageAccount storageAccount)
        {
            List<string> recentlyExecutedAvailabilitySetDomainCombination = new List<string>();
            Dictionary<string, int> possibleAvailabilitySetDomainCombinationVMCount = new Dictionary<string, int>();
            var meanTimeQuery = TableQuery.GenerateFilterConditionForDate("scheduledExecutionTime", QueryComparisons.GreaterThanOrEqual, DateTimeOffset.UtcNow.AddMinutes(-120));
            var recentlyExecutedAvailabilitySetDomainCombinationQuery = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, VirtualMachineGroup.AvailabilitySets.ToString());
            var recentlyExecutedFinalAvailabilitySetDomainQuery = TableQuery.CombineFilters(meanTimeQuery, TableOperators.And, recentlyExecutedAvailabilitySetDomainCombinationQuery);
            TableQuery<ScheduledRulesEntity> scheduledQuery = new TableQuery<ScheduledRulesEntity>().Where(recentlyExecutedFinalAvailabilitySetDomainQuery);
            var executedAvilabilitySetCombinationResults = storageAccountProvider.GetEntities<ScheduledRulesEntity>(scheduledQuery, storageAccount, azureSettings.ScheduledRulesTable);
            if (executedAvilabilitySetCombinationResults != null)
            {
                foreach (var eachExecutedAvilabilitySetCombinationResults in executedAvilabilitySetCombinationResults)
                {
                    if (azureSettings.Chaos.AvailabilitySetChaos.FaultDomainEnabled)
                    {
                        if (eachExecutedAvilabilitySetCombinationResults.combinationKey.Contains("!"))
                        {
                            if (possibleAvailabilitySetDomainCombinationVMCount.ContainsKey(eachExecutedAvilabilitySetCombinationResults.combinationKey))
                            {
                                possibleAvailabilitySetDomainCombinationVMCount[eachExecutedAvilabilitySetCombinationResults.combinationKey] += 1;
                            }
                            else
                            {
                                possibleAvailabilitySetDomainCombinationVMCount[eachExecutedAvilabilitySetCombinationResults.combinationKey] = 1;
                            }
                        }
                    }
                    else
                    {
                        if (eachExecutedAvilabilitySetCombinationResults.combinationKey.Contains("@"))
                        {
                            if (possibleAvailabilitySetDomainCombinationVMCount.ContainsKey(eachExecutedAvilabilitySetCombinationResults.combinationKey))
                            {
                                possibleAvailabilitySetDomainCombinationVMCount[eachExecutedAvilabilitySetCombinationResults.combinationKey] += 1;
                            }
                            else
                            {
                                possibleAvailabilitySetDomainCombinationVMCount[eachExecutedAvilabilitySetCombinationResults.combinationKey] = 1;
                            }
                        }
                    }
                }
                recentlyExecutedAvailabilitySetDomainCombination = new List<string>(possibleAvailabilitySetDomainCombinationVMCount.Keys);
            }
            return recentlyExecutedAvailabilitySetDomainCombination;
        }
        private List<string> GetPossibleAvailabilitySets(IStorageAccountProvider storageAccountProvider, CloudStorageAccount storageAccount)
        {
            List<string> possibleAvailableSets = new List<string>();
            var availabilitySetQuery = TableQuery.GenerateFilterCondition("Virtualmachines", QueryComparisons.NotEqual, "");
            TableQuery<AvailabilitySetsCrawlerResponseEntity> AvailabilitySetTableQuery = new TableQuery<AvailabilitySetsCrawlerResponseEntity>().Where(availabilitySetQuery);

            var crawledAvailabilitySetResults = storageAccountProvider.GetEntities<AvailabilitySetsCrawlerResponseEntity>(AvailabilitySetTableQuery, storageAccount, azureSettings.AvailabilitySetCrawlerTableName);
            if (crawledAvailabilitySetResults == null)
            {
                return null;
            }

            Dictionary<string, int> possibleAvailabilitySetDomainCombinationVMCount = new Dictionary<string, int>();
            var bootStrapQuery = string.Empty;
            bool initialQuery = true;
            foreach (var eachAvailabilitySet in crawledAvailabilitySetResults)
            {
                if (initialQuery)
                {
                    bootStrapQuery = TableQuery.GenerateFilterCondition("AvailableSetId", QueryComparisons.Equal, ConvertToProperAvailableSetId(eachAvailabilitySet.RowKey));
                    initialQuery = false;
                }
                else
                {
                    var localAvailabilitySetQuery = TableQuery.GenerateFilterCondition("AvailableSetId", QueryComparisons.Equal, ConvertToProperAvailableSetId(eachAvailabilitySet.RowKey));
                    bootStrapQuery = TableQuery.CombineFilters(localAvailabilitySetQuery, TableOperators.Or, bootStrapQuery);
                }
            }

            TableQuery<VirtualMachineCrawlerResponseEntity> VirtualMachineTableQuery = new TableQuery<VirtualMachineCrawlerResponseEntity>().Where(bootStrapQuery);
            var crawledVirtualMachineResults = storageAccountProvider.GetEntities<VirtualMachineCrawlerResponseEntity>(VirtualMachineTableQuery, storageAccount, azureSettings.VirtualMachineCrawlerTableName);
            foreach (var eachVirtualMachine in crawledVirtualMachineResults)
            {
                var entryIntoPossibleAvailabilitySetDomainCombinationVMCount = string.Empty;
                if (azureSettings.Chaos.AvailabilitySetChaos.FaultDomainEnabled)
                {
                    entryIntoPossibleAvailabilitySetDomainCombinationVMCount = eachVirtualMachine.AvailableSetId + "@" + eachVirtualMachine.FaultDomain.ToString();
                }
                else
                {
                    entryIntoPossibleAvailabilitySetDomainCombinationVMCount = eachVirtualMachine.AvailableSetId + "@" + eachVirtualMachine.UpdateDomain.ToString();
                }

                if (possibleAvailabilitySetDomainCombinationVMCount.ContainsKey(entryIntoPossibleAvailabilitySetDomainCombinationVMCount))
                {
                    possibleAvailabilitySetDomainCombinationVMCount[entryIntoPossibleAvailabilitySetDomainCombinationVMCount] += 1;
                }
                else
                {
                    possibleAvailabilitySetDomainCombinationVMCount[entryIntoPossibleAvailabilitySetDomainCombinationVMCount] = 1;
                }
            }

            possibleAvailableSets = new List<string>(possibleAvailabilitySetDomainCombinationVMCount.Keys);
            return possibleAvailableSets;
        }

        private string ConvertToProperAvailableSetId(string rowKey)
        {
            var rowKeyChunks = rowKey.Split('!');
            return string.Join("/", rowKeyChunks).Replace(rowKeyChunks.Last(), rowKeyChunks.Last().ToUpper());
        }

        public Task CreateRuleAsync(AzureClient azureClient)
        {
            throw new NotImplementedException();
        }
    }
}
