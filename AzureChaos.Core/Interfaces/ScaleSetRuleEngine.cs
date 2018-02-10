using AzureChaos.Entity;
using AzureChaos.Helper;
using AzureChaos.Models;
using AzureChaos.Providers;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureChaos.Interfaces
{
    //TODO : Exception ending and logging
    /// <summary>Scale set rule engine will create the rules for the virtual machine based on the config settings and existing schedule/event tables.</summary>
    public class ScaleSetRuleEngine : IRuleEngine
    {
        private AzureSettings azureSettings;

        /// <summary>Check whether chaos enabled for the virtual machine scale set and over all</summary>
        /// <param name="azureSettings">config values </param>
        /// <returns></returns>
        public bool IsChaosEnabled(AzureSettings azureSettings)
        {
            if (azureSettings?.Chaos == null || !azureSettings.Chaos.ChaosEnabled ||
                azureSettings.Chaos.AvailabilitySetChaos == null || !azureSettings.Chaos.AvailabilitySetChaos.Enabled)
            {
                return false;
            }

            return true;
        }

        /// <summary>Create the rule for the virtual machine scale vms </summary>
        /// <param name="azureClient"></param>
        public void CreateRule(AzureClient azureClient)
        {
            azureSettings = azureClient.azureSettings;
            if (IsChaosEnabled(azureSettings))
            {
                IStorageAccountProvider storageAccountProvider = new StorageAccountProvider();
                var storageAccount = storageAccountProvider.CreateOrGetStorageAccount(azureClient);
                var scaleSet = GetRandomScaleSet(storageAccountProvider, azureSettings, storageAccount);
                var filteredVmSet = GetVirtualMachineSet(storageAccountProvider, azureSettings, storageAccount, scaleSet.Id);
                if (filteredVmSet == null)
                {
                    return;
                }

                TableBatchOperation batchOperation = null;
                CloudTable table = storageAccountProvider.CreateOrGetTable(storageAccount, azureSettings.ScheduledRulesTable);
                var count = VmCount(filteredVmSet.Count, azureSettings);

                do
                {
                    filteredVmSet = filteredVmSet.Take(count).ToList();
                    batchOperation = VirtualMachineHelper.CreateScheduleEntity(filteredVmSet, azureSettings.Chaos.SchedulerFrequency);
                    if (batchOperation != null || batchOperation.Count > 0)
                    {
                        Extensions.Synchronize(() => table.ExecuteBatchAsync(batchOperation));
                    }
                } while (filteredVmSet != null && filteredVmSet.Any());
            }
        }

        /// <summary>Pick the random scale set.</summary>
        /// <param name="storageAccountProvider">storage provider to access the common storage functions.</param>
        /// <param name="azureSettings">azure settings the config values which were read from the blob</param>
        /// <param name="storageAccount">storage account to access the storage table and other properties</param>
        /// <returns></returns>
        private VMScaleSetCrawlerResponseEntity GetRandomScaleSet(IStorageAccountProvider storageAccountProvider, AzureSettings azureSettings, CloudStorageAccount storageAccount)
        {
            TableQuery<VMScaleSetCrawlerResponseEntity> vmScaleSetsQuery = new TableQuery<VMScaleSetCrawlerResponseEntity>();
            var resultsSet = ResourceFilterHelper.QueryByMeanTime<VMScaleSetCrawlerResponseEntity>(storageAccount, storageAccountProvider, azureSettings,
               azureSettings.ScaleSetCrawlerTableName);
            if (resultsSet == null || !resultsSet.Any())
            {
                return null;
            }

            Random random = new Random();
            var randomScaleSetIndex = random.Next(0, resultsSet.Count());
            return resultsSet.ToArray()[randomScaleSetIndex];
        }

        /// <summary>Get the list of virtual machines, based on the precondition check on the schedule table and activity table.
        /// here precondion ==> get the virtual machines from the crawler which are not in the recent scheduled list and not in the recent activities.</summary>
        /// <param name="storageAccountProvider">storage provider to access the common storage functions.</param>
        /// <param name="azureSettings">azure settings the config values which were read from the blob</param>
        /// <param name="storageAccount">storage account to access the storage table and other properties</param>
        /// <param name="scaleSetId">scale set id to filter the virtual machines.</param>
        /// <returns></returns>
        private IList<VirtualMachineCrawlerResponseEntity> GetVirtualMachineSet(IStorageAccountProvider storageAccountProvider, AzureSettings azureSettings,
            CloudStorageAccount storageAccount, string scaleSetId)
        {
            /// Get the standlone virtual machines
            var partitionKey = scaleSetId.Replace('/', '!');
            var groupNameFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey);
            var resultsSet = ResourceFilterHelper.QueryByMeanTime<VirtualMachineCrawlerResponseEntity>(storageAccount, storageAccountProvider, azureSettings,
                azureSettings.VirtualMachineCrawlerTableName, groupNameFilter);
            if (resultsSet == null || !resultsSet.Any())
            {
                return null;
            }

            var scheduleEntities = ResourceFilterHelper.QueryByMeanTime<ScheduledRulesEntity>(storageAccount, storageAccountProvider, azureSettings, azureSettings.ScheduledRulesTable);
            var scheduleEntitiesResourceIds = (scheduleEntities == null || !scheduleEntities.Any()) ? new List<string>() : scheduleEntities.Select(x => x.RowKey.Replace("!", "/"));

            var activityEntities = ResourceFilterHelper.QueryByMeanTime<EventActivityEntity>(storageAccount, storageAccountProvider, azureSettings, azureSettings.ActivityLogTable);
            var activityEntitiesResourceIds = (activityEntities == null || !activityEntities.Any()) ? new List<string>() : activityEntities.Select(x => x.Id);

            var result = resultsSet.Where(x => !scheduleEntitiesResourceIds.Contains(x.Id) && !activityEntitiesResourceIds.Contains(x.Id));
            return result == null ? null : result.ToList();
        }

        /// <summary>Get the virtual machine count based on the config percentage.</summary>
        /// <param name="totalCount">Total number of the virual machines.</param>
        /// <param name="azureSettings">Azure configuration</param>
        /// <returns></returns>
        private int VmCount(int totalCount, AzureSettings azureSettings)
        {
            var vmPercentage = azureSettings?.Chaos?.ScaleSetChaos?.percentageTermination;

            /// How do we design the model for dynamic name ex. how do read deseriliaze the config value  microsoft.chaos.SS.<SSname>.enabled
            /*    vmPercentage = azureClient?.azureSettings?.Chaos?.ScaleSetChaos.percentageTermination if (vmPercentage == null || vmPercentage <= 0)
               {
               }*/
            return (int)((vmPercentage / 100) * totalCount);
        }

        public Task CreateRuleAsync(AzureClient azureClient)
        {
            throw new NotImplementedException();
        }
    }
}
