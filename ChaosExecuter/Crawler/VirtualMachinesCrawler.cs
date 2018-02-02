using AzureChaos;
using AzureChaos.Entity;
using AzureChaos.Models;
using AzureChaos.Providers;
using AzureChaos.Helper;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Fluent;
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
    public static class VirtualMachinesCrawler
    {
        private static ADConfiguration config = new ADConfiguration();
        private static StorageAccountProvider storageProvider = new StorageAccountProvider(config);
        private static CloudTableClient tableClient = storageProvider.tableClient;
        private static CloudTable table = storageProvider.CreateOrGetTable("VirtualMachineTable");


        [FunctionName("VirtualMachinesCrawler")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "GetVirtualMachines")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("VirtualMachinesCrawler function processed a request.");
            try
            {
                TableBatchOperation batchOperation = new TableBatchOperation();
                IAzure azure_client = AzureClient.GetAzure(config);
                List<string> loadBalancersVms = await GetVirtualMachinesFromLoadBalancers(azure_client);
                // will be listing out only the standalone virtual machines.
                List<string> resourceGroupList = ResourceGroupHelper.GetResourceGroupsInSubscription(config);
                foreach (string resourceGroup in resourceGroupList)
                {
                    var virtualMachines = azure_client.VirtualMachines.ListByResourceGroup(resourceGroup).Where(x => string.IsNullOrWhiteSpace(x.AvailabilitySetId)
                && !loadBalancersVms.Contains(x.Id, StringComparer.OrdinalIgnoreCase));
                    foreach (IVirtualMachine virtualMachine in virtualMachines)
                    {
                        batchOperation.Insert(ConvertToVirtualMachineEntity(virtualMachine));
                    }
                }
                //var virtualMachines = azure_client.VirtualMachines.ListByResourceGroup(config.ResourceGroup).Where(x => string.IsNullOrWhiteSpace(x.AvailabilitySetId)
                //&& !loadBalancersVms.Contains(x.Id, StringComparer.OrdinalIgnoreCase));
                //foreach (var virtualMachine in virtualMachines)
                //{
                //    batchOperation.Insert(ConvertToVirtualMachineEntity(virtualMachine));
                //}

                await Task.Factory.StartNew(() =>
                {
                    table.ExecuteBatch(batchOperation);
                });
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
        private static async Task<List<string>> GetVirtualMachinesFromLoadBalancers(IAzure azure_client)
        {
            var vmIds = new List<string>();
            var pagedCollection = await azure_client.LoadBalancers.ListAsync();
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

        /// <summary>Convert the Virtual machine to virtual machine crawler response entity.</summary>
        /// <param name="virtualMachine">The virtual machine.</param>
        /// <param name="vmGroupName">Vm group name.</param>
        /// <returns></returns>
        private static VirtualMachineCrawlerResponseEntity ConvertToVirtualMachineEntity(IVirtualMachine virtualMachine, string vmGroupName = "")
        {
            vmGroupName = string.IsNullOrWhiteSpace(vmGroupName) ? virtualMachine.Type : vmGroupName;
            VirtualMachineCrawlerResponseEntity virtualMachineCrawlerResponseEntity = new VirtualMachineCrawlerResponseEntity(config.ResourceGroup, Guid.NewGuid().ToString());
            virtualMachineCrawlerResponseEntity.EntryInsertionTime = DateTime.Now;
            //resourceGroupCrawlerResponseEntity.EventType = data?.Action;
            virtualMachineCrawlerResponseEntity.RegionName = virtualMachine.RegionName;
            virtualMachineCrawlerResponseEntity.ResourceGroupName = virtualMachine.ResourceGroupName;
            virtualMachineCrawlerResponseEntity.ResourceName = virtualMachine.Name;
            virtualMachineCrawlerResponseEntity.AvailableSetId = virtualMachine.AvailabilitySetId;
            virtualMachineCrawlerResponseEntity.ResourceType = virtualMachine.Type;
            return virtualMachineCrawlerResponseEntity;
        }
    }
}
