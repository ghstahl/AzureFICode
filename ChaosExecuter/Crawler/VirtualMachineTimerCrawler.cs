using AzureChaos.Core.Helper;
using AzureChaos.Core.Models;
using AzureChaos.Core.Providers;
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
                var azureSettings = AzureClient.AzureSettings;

                // will be listing out only the standalone virtual machines.
                var resourceGroupList = ResourceGroupHelper.GetResourceGroupsInSubscription(AzureClient.AzureInstance, azureSettings);
                if (resourceGroupList == null)
                {
                    log.Info($"timercrawlerforvirtualmachines: no resource groups to crawl");
                    return;
                }

                foreach (var resourceGroup in resourceGroupList)
                {
                    var loadBalancersVms = await GetVirtualMachinesFromLoadBalancers(resourceGroup.Name, log);
                    var pagedCollection = await AzureClient.AzureInstance.VirtualMachines.ListByResourceGroupAsync(resourceGroup.Name);
                    if (pagedCollection == null || !pagedCollection.Any())
                    {
                        continue;
                    }

                    var storageAccount = StorageProvider.CreateOrGetStorageAccount(AzureClient);
                    var virtualMachines = pagedCollection.Select(x => x).Where(x => string.IsNullOrWhiteSpace(x.AvailabilitySetId) &&
                    !loadBalancersVms.Contains(x.Id, StringComparer.OrdinalIgnoreCase));
                    var machines = virtualMachines.ToList();
                    if(!machines.Any())
                    {
                        return;
                    }

                    var vmList = machines.ToList();
                    var groupByResourceGroupName = vmList.GroupBy(x => x.ResourceGroupName).ToList();
                    foreach (var groupItem in groupByResourceGroupName)
                    {
                        TableBatchOperation batchOperation = new TableBatchOperation();
                        foreach (var virtualMachine in groupItem)
                        {
                            batchOperation.InsertOrReplace(VirtualMachineHelper.ConvertToVirtualMachineEntity(virtualMachine, virtualMachine.ResourceGroupName));
                        }

                        if (batchOperation.Count <= 0) continue;
                        var table = await StorageProvider.CreateOrGetTableAsync(storageAccount, azureSettings.VirtualMachineCrawlerTableName);
                        await table.ExecuteBatchAsync(batchOperation);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"timercrawlerforvirtualmachines threw the exception ", ex, "timercrawlerforvirtualmachines");
            }
        }

        /// <summary>Get the list of </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="log"></param>
        /// <returns>Returns the list of vm ids which are in the load balancers.</returns>
        private static async Task<List<string>> GetVirtualMachinesFromLoadBalancers(string resourceGroup, TraceWriter log)
        {
            log.Info($"timercrawlerforvirtualmachines getting the load balancer virtual machines");
            var vmIds = new List<string>();
            var pagedCollection = await AzureClient.AzureInstance.LoadBalancers.ListByResourceGroupAsync(resourceGroup);
            if (pagedCollection == null)
            {
                return vmIds;
            }

            var loadBalancers = pagedCollection.Select(x => x);
            var balancers = loadBalancers.ToList();
            if (!balancers.Any())
            {
                return vmIds;
            }
            vmIds.AddRange(balancers.SelectMany(x => x.Backends).SelectMany(x => x.Value.GetVirtualMachineIds()));
            return vmIds;
        }
    }
}