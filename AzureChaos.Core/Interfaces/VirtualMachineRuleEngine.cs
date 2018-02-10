using AzureChaos.Entity;
using AzureChaos.Enums;
using AzureChaos.Helper;
using AzureChaos.Models;
using AzureChaos.Providers;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureChaos.Interfaces
{
    // TODO : Exception ending and logging
    /// <summary>Virtual machine rule engine will create the rules for the virtual machine based on the config settings and existing schedule/event tables.</summary>
    public class VirtualMachineRuleEngine : IRuleEngine
    {
        /// <summary>Check whether chaos enabled for the virtual machine and over all</summary>
        /// <param name="azureSettings">config values </param>
        /// <returns></returns>
        public bool IsChaosEnabled(AzureSettings azureSettings)
        {
            if (azureSettings?.Chaos == null || !azureSettings.Chaos.ChaosEnabled ||
              azureSettings.Chaos.VirtualMachineChaos == null || !azureSettings.Chaos.VirtualMachineChaos.Enabled)
            {
                return false;
            }

            return true;
        }

        /// <summary>Create the virtual machine rules</summary>
        /// <param name="azureClient"></param>
        public void CreateRule(AzureClient azureClient)
        {
            var azureSettings = azureClient.azureSettings;
            if (!IsChaosEnabled(azureSettings))
            {
                return;
            }

            IStorageAccountProvider storageAccountProvider = new StorageAccountProvider();
            var storageAccount = storageAccountProvider.CreateOrGetStorageAccount(azureClient);
            var vmSets = GetRandomVmSet(storageAccountProvider, azureSettings, storageAccount);
            if (vmSets == null)
            {
                return;
            }

            TableBatchOperation batchOperation = null;
            CloudTable table = storageAccountProvider.CreateOrGetTable(storageAccount, azureSettings.ScheduledRulesTable);
            var count = VmCount(vmSets.Count, azureSettings);
            do
            {
                vmSets = vmSets.Take(count).ToList();
                batchOperation = VirtualMachineHelper.CreateScheduleEntity(vmSets, azureSettings.Chaos.SchedulerFrequency);
                if (batchOperation != null || batchOperation.Count > 0)
                {
                    Extensions.Synchronize(() => table.ExecuteBatchAsync(batchOperation));
                }
            } while (vmSets != null && vmSets.Any());

        }

        /// <summary>Get the list of virtual machines, based on the preconditioncheck on the schedule table and activity table.
        /// here precondion ==> get the virtual machines from the crawler which are not in the recent scheduled list and not in the recent activities.</summary>
        /// <param name="storageAccountProvider"> storage provider to access the common storage functions.</param>
        /// <param name="azureSettings">azure settings the config values which were read from the blob</param>
        /// <param name="storageAccount">storage account to access the storage table and other properties</param>
        /// <returns></returns>
        private IList<VirtualMachineCrawlerResponseEntity> GetRandomVmSet(IStorageAccountProvider storageAccountProvider, AzureSettings azureSettings,
            CloudStorageAccount storageAccount)
        {
            /// Get the standlone virtual machines
            var groupNameFilter = TableQuery.GenerateFilterCondition("VirtualMachineGroup", QueryComparisons.Equal, VirtualMachineGroup.VirtualMachines.ToString());
            var resultsSet = ResourceFilterHelper.QueryByMeanTime<VirtualMachineCrawlerResponseEntity>(storageAccount, storageAccountProvider, azureSettings,
                azureSettings.VirtualMachineCrawlerTableName, groupNameFilter);
            if (resultsSet == null || !resultsSet.Any())
            {
                return null;
            }

            // TODO: do we take percentage of vm's in random or sequential?
            ResourceFilterHelper.Shuffle<VirtualMachineCrawlerResponseEntity>(resultsSet);

            /// Get schedule entities
            var scheduleEntities = ResourceFilterHelper.QueryByMeanTime<ScheduledRulesEntity>(storageAccount, storageAccountProvider, azureSettings,
                azureSettings.ScheduledRulesTable);
            var scheduleEntitiesResourceIds = (scheduleEntities == null || !scheduleEntities.Any()) ? new List<string>() :
                scheduleEntities.Select(x => x.RowKey.Replace("!", "/"));

            /// Get activity entities
            var activityEntities = ResourceFilterHelper.QueryByMeanTime<EventActivityEntity>(storageAccount, storageAccountProvider, azureSettings,
                azureSettings.ActivityLogTable);
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
            var vmPercentage = azureSettings?.Chaos?.VirtualMachineChaos?.percentageTermination;
            return (int)((vmPercentage / 100) * totalCount);
        }

        public Task CreateRuleAsync(AzureClient azureClient)
        {
            throw new System.NotImplementedException();
        }
    }
}
