using AzureChaos.Helper;
using AzureChaos.Models;
using AzureChaos.Providers;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChaosExecuter.Crawler
{
    public static class VirtualMachineTimerCrawler
    {
        private static readonly AzureClient AzureClient = new AzureClient();
        private static readonly IStorageAccountProvider StorageProvider = new StorageAccountProvider();

        [FunctionName("timercrawlerforvirtualmachines")]
        public static async void Run([TimerTrigger("0 */15 * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"timercrawlerforvirtualmachines executed at: {DateTime.UtcNow}");
            try
            {
                var azureSettings = AzureClient.azureSettings;

                // will be listing out only the standalone virtual machines.
                var resourceGroupList = ResourceGroupHelper.GetResourceGroupsInSubscription(AzureClient.azure, azureSettings);
                if (resourceGroupList == null)
                {
                    log.Info($"timercrawlerforvirtualmachines: no resource groups to crawl");
                    return;
                }

                foreach (var resourceGroup in resourceGroupList)
                {
                    List<string> loadBalancersVms = await GetVirtualMachinesFromLoadBalancers(resourceGroup.Name, log);
                    var pagedCollection = await AzureClient.azure.VirtualMachines.ListByResourceGroupAsync(resourceGroup.Name);
                    if (pagedCollection == null || !pagedCollection.Any())
                    {
                        continue;
                    }

                    var storageAccount = StorageProvider.CreateOrGetStorageAccount(AzureClient);
                    var virtualMachines = pagedCollection.Select(x => x).Where(x => string.IsNullOrWhiteSpace(x.AvailabilitySetId) &&
                    !loadBalancersVms.Contains(x.Id, StringComparer.OrdinalIgnoreCase));
                    if(virtualMachines == null || !virtualMachines.Any())
                    {
                        return;
                    }

                    /// Unique key of the table entry will be calculated based on the resourcegroup(as partitionkey) and resource name(as row key).
                    /// So here grouping is needed, since vm groups can be different, table batch operation will not be allowing the different partition keys(i.e. group name can be different) in one batch operation
                    var groupByResourceGroupName = virtualMachines.GroupBy(x => x.ResourceGroupName).ToList();
                    foreach (var groupItem in groupByResourceGroupName)
                    {
                        TableBatchOperation batchOperation = new TableBatchOperation();
                        foreach (IVirtualMachine virtualMachine in groupItem)
                        {
                            batchOperation.InsertOrReplace(VirtualMachineHelper.ConvertToVirtualMachineEntity(virtualMachine, virtualMachine.ResourceGroupName));
                        }

                        if (batchOperation.Count > 0)
                        {
                            CloudTable table = await StorageProvider.CreateOrGetTableAsync(storageAccount, azureSettings.VirtualMachineCrawlerTableName);
                            await table.ExecuteBatchAsync(batchOperation);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"timercrawlerforvirtualmachines threw the exception ", ex, "timercrawlerforvirtualmachines");
            }
        }

        /// <summary>Get the list of </summary>
        /// <param name="azure_client"></param>
        /// <returns>Returns the list of vm ids which are in the load balancers.</returns>
        private static async Task<List<string>> GetVirtualMachinesFromLoadBalancers(string resourceGroup, TraceWriter log)
        {
            log.Info($"timercrawlerforvirtualmachines getting the load balancer virtual machines");
            var vmIds = new List<string>();
            var pagedCollection = await AzureClient.azure.LoadBalancers.ListByResourceGroupAsync(resourceGroup);
            if (pagedCollection == null)
            {
                return vmIds;
            }

            var loadBalancers = pagedCollection.Select(x => x);
            if (loadBalancers == null || !loadBalancers.Any())
            {
                return vmIds;
            }
            vmIds.AddRange(loadBalancers.SelectMany(x => x.Backends).SelectMany(x => x.Value.GetVirtualMachineIds()));
            return vmIds;
        }
    }
}