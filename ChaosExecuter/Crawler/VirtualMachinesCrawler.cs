using AzureChaos.Helper;
using AzureChaos.Models;
using AzureChaos.Providers;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace ChaosExecuter.Crawler
{
    /// <summary>Virtual machines crawler</summary>
    public static class VirtualMachinesCrawler
    {
        private static readonly AzureClient AzureClient = new AzureClient();
        private static readonly IStorageAccountProvider StorageProvider = new StorageAccountProvider();

        /// <summary>Crawl the virtual machines under the all the resource group.</summary>
        /// <param name="req">The http request</param>
        /// <param name="log">Trace logger instance.</param>
        /// <returns></returns>
        [FunctionName("crawlvirtualmachines")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "crawlvirtualmachines")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("VirtualMachinesCrawler function processed a request.");
            try
            {
                TableBatchOperation batchOperation = new TableBatchOperation();
                // will be listing out only the standalone virtual machines.
                List<string> resourceGroupList = ResourceGroupHelper.GetResourceGroupsInSubscription(AzureClient.azure);
                foreach (var resourceGroup in resourceGroupList)
                {
                    List<string> loadBalancersVms = await GetVirtualMachinesFromLoadBalancers(resourceGroup);
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
                log.Error($"VirtualMachines Crawler function Throw the exception ", ex, "VirtualMachinesCrawler");
                return req.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }

        /// <summary>Get the list of </summary>
        /// <param name="azure_client"></param>
        /// <returns>Returns the list of vm ids which are in the load balancers.</returns>
        private static async Task<List<string>> GetVirtualMachinesFromLoadBalancers(string resourceGroup)
        {
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