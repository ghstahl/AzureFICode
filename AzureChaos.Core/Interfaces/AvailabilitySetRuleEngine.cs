using AzureChaos.Entity;
using AzureChaos.Models;
using AzureChaos.Providers;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzureChaos.Helper;
using AzureChaos.Enums;

namespace AzureChaos.Interfaces
{
    public class AvailabilitySetRuleEngine : IRuleEngine
    {
        //private IStorageAccountProvider storageAccountProvider;
        //private CloudStorageAccount storageAccount;
        private AzureSettings azureSettings;
        // mints here  --  60
        // getting resources from crawler tables, which was not in the activity table within the 60mints.
        // Random pick based on the configuration, and keeep those schedule time will be the same.
        //
        // add entries into the scheduler, along with action details - i.e. based on the final state of the resources,
        // ex. if it is Running, then action will PowerOff
        // with precondition check on the schedule table against that particular resource.

        //public AvailabilitySetRuleEngine(AzureClient azureClient, IStorageAccountProvider storageAccountProvider, CloudStorageAccount storageAccount)
        //{
        //    this.storageAccountProvider = storageAccountProvider;
        //    this.storageAccount = storageAccount;
        //}

        public void CreateRule(AzureClient azureClient)
        {
            azureSettings = azureClient.azureSettings;
            Random random = new Random();
            IStorageAccountProvider storageAccountProvider = new StorageAccountProvider();
            var storageAccount = storageAccountProvider.CreateOrGetStorageAccount(azureClient);
            //1) OpenSearch with Vm Count > 0
            var possibleAvailabilitySets = GetPossibleAvailabilitySets(storageAccountProvider, storageAccount);
            var recentlyExcludedAvailabilitySetDomainCombination = GetRecentlyExecutedAvailabilitySetDomainCombination(storageAccountProvider, storageAccount);
            var availableSetDomainOptions = possibleAvailabilitySets.Except(recentlyExcludedAvailabilitySetDomainCombination).ToList();
            var randomAvailabilitySetDomainCombination = availableSetDomainOptions[random.Next(0, availableSetDomainOptions.Count - 1)];
            var componentsInAvailabilitySetDomainCombination = randomAvailabilitySetDomainCombination.Split('@');
            var domainNumber = int.Parse(componentsInAvailabilitySetDomainCombination.Last());
            var availabilitySetId = componentsInAvailabilitySetDomainCombination.First();
            InsertVirtualMachineAvailabilitySetDomainResults(storageAccountProvider, storageAccount, availabilitySetId, domainNumber);
            
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
            if (crawledAvailabilitySetResults != null)
            {
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
            }
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

        public bool IsChaosEnabled(AzureSettings azureSettings)
        {
            if (azureSettings?.Chaos == null || !azureSettings.Chaos.ChaosEnabled
                || azureSettings.Chaos.AvailabilitySetChaos == null || !azureSettings.Chaos.AvailabilitySetChaos.Enabled)
            {
                return false;
            }

            return true;
        }


    }
}
