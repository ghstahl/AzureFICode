using AzureChaos.Core.Entity;
using AzureChaos.Core.Enums;
using AzureChaos.Core.Helper;
using AzureChaos.Core.Models;
using AzureChaos.Core.Models.Configs;
using AzureChaos.Core.Providers;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureChaos.Core.Interfaces
{
    // TODO : Exception ending and logging
    /// <summary>Virtual machine rule engine will create the rules for the virtual machine based on the config settings and existing schedule/event tables.</summary>
    public class VirtualMachineRuleEngine : IRuleEngine
    {
        /// <summary>Create the virtual machine rules</summary>
        /// <param name="azureClient"></param>
        /// <param name="log"></param>
        public void CreateRule(AzureClient azureClient, TraceWriter log)
        {
            try
            {
                log.Info("VirtualMachine RuleEngine: Started the creating rules for the virtual machines.");
                var azureSettings = azureClient.AzureSettings;
                IStorageAccountProvider storageAccountProvider = new StorageAccountProvider();
                var storageAccount = storageAccountProvider.CreateOrGetStorageAccount(azureClient);
                var vmSets = GetRandomVmSet(storageAccountProvider, azureSettings, storageAccount);
                if (vmSets == null)
                {
                    log.Info("VirtualMachine RuleEngine: No virtual machines found..");
                    return;
                }

                CloudTable table = storageAccountProvider.CreateOrGetTable(storageAccount, azureSettings.ScheduledRulesTable);
                var count = VmCount(vmSets.Count, azureSettings);
                do
                {
                    var randomSets = vmSets.Take(count).ToList();
                    vmSets = vmSets.Except(randomSets).ToList();
                    var batchOperation = VirtualMachineHelper.CreateScheduleEntity(randomSets, azureSettings.Chaos.SchedulerFrequency, VirtualMachineGroup.VirtualMachines);
                    if (batchOperation == null) continue;

                    var operation = batchOperation;
                    Extensions.Synchronize(() => table.ExecuteBatchAsync(operation));
                } while (vmSets != null && vmSets.Any());

                log.Info("VirtualMachine RuleEngine: Completed creating rule engine..");
            }
            catch (Exception ex)
            {
                log.Error("VirtualMachine RuleEngine: Exception thrown. ", ex);
            }
        }

        /// <summary>Get the list of virtual machines, based on the preconditioncheck on the schedule table and activity table.
        /// here precondion ==> get the virtual machines from the crawler which are not in the recent scheduled list and not in the recent activities.</summary>
        /// <param name="storageAccountProvider"> storage provider to access the common storage functions.</param>
        /// <param name="azureSettings">azure settings the config values which were read from the blob</param>
        /// <param name="storageAccount">storage account to access the storage table and other properties</param>
        /// <returns></returns>
        private static IList<VirtualMachineCrawlerResponse> GetRandomVmSet(IStorageAccountProvider storageAccountProvider, AzureSettings azureSettings,
            CloudStorageAccount storageAccount)
        {
            var groupNameFilter = TableQuery.GenerateFilterCondition("VirtualMachineGroup", QueryComparisons.Equal, VirtualMachineGroup.VirtualMachines.ToString());
            var resultsSet = ResourceFilterHelper.QueryByMeanTime<VirtualMachineCrawlerResponse>(storageAccount, storageAccountProvider, azureSettings,
                azureSettings.VirtualMachineCrawlerTableName, groupNameFilter);
            if (resultsSet == null || !resultsSet.Any())
            {
                return null;
            }

            // TODO: do we take percentage of vm's in random or sequential?
            ResourceFilterHelper.Shuffle(resultsSet);

            var scheduleEntities = ResourceFilterHelper.QueryByMeanTime<ScheduledRules>(storageAccount, storageAccountProvider, azureSettings,
                azureSettings.ScheduledRulesTable);
            var scheduleEntitiesResourceIds = scheduleEntities == null || !scheduleEntities.Any() ? new List<string>() :
                scheduleEntities.Select(x => x.RowKey.Replace("!", "/"));

            var activityEntities = ResourceFilterHelper.QueryByMeanTime<EventActivity>(storageAccount, storageAccountProvider, azureSettings,
                azureSettings.ActivityLogTable);
            var activityEntitiesResourceIds = activityEntities == null || !activityEntities.Any() ? new List<string>() : activityEntities.Select(x => x.Id);

            var result = resultsSet.Where(x => !scheduleEntitiesResourceIds.Contains(x.Id) && !activityEntitiesResourceIds.Contains(x.Id));
            return result.ToList();
        }

        /// <summary>Get the virtual machine count based on the config percentage.</summary>
        /// <param name="totalCount">Total number of the virual machines.</param>
        /// <param name="azureSettings">Azure configuration</param>
        /// <returns></returns>
        private static int VmCount(int totalCount, AzureSettings azureSettings)
        {
            var vmPercentage = azureSettings?.Chaos?.VirtualMachineChaos?.PercentageTermination;
            return vmPercentage != null ? (int)(vmPercentage / 100 * totalCount) : totalCount;
        }

        public Task CreateRuleAsync(AzureClient azureClient)
        {
            throw new NotImplementedException();
        }
    }
}
