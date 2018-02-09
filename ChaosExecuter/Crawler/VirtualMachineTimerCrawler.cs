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
            log.Info($"timercrawlerforvirtualmachines executed at: {DateTime.Now}");
            try
            {
                TableBatchOperation batchOperation = new TableBatchOperation();
                // will be listing out only the standalone virtual machines.
                List<string> resourceGroupList = ResourceGroupHelper.GetResourceGroupsInSubscription(AzureClient.azure);
                foreach (string resourceGroup in resourceGroupList)
                {
                    List<string> loadBalancersVms = await GetVirtualMachinesFromLoadBalancers(resourceGroup, log);
                    var pagedCollection = await AzureClient.azure.VirtualMachines.ListByResourceGroupAsync(resourceGroup);
                    if (pagedCollection == null || !pagedCollection.Any())
                    {
                        continue;
                    }

                    var virtualMachines = pagedCollection.Select(x => x).Where(x => string.IsNullOrWhiteSpace(x.AvailabilitySetId) &&
                    !loadBalancersVms.Contains(x.Id, StringComparer.OrdinalIgnoreCase));
                    foreach (IVirtualMachine virtualMachine in virtualMachines)
                    {
                        batchOperation.InsertOrReplace(VirtualMachineHelper.ConvertToVirtualMachineEntity(virtualMachine));
                    }
                }

                var storageAccount = StorageProvider.CreateOrGetStorageAccount(AzureClient);
                if (batchOperation.Count > 0)
                {
                    CloudTable table = await StorageProvider.CreateOrGetTableAsync(storageAccount, AzureClient.VirtualMachineCrawlerTableName);
                    await table.ExecuteBatchAsync(batchOperation);
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