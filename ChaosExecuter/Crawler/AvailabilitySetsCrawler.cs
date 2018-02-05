using AzureChaos.Entity;
using AzureChaos.Enums;
using AzureChaos.Helper;
using AzureChaos.Models;
using AzureChaos.Providers;
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
    public static class AvailabilitySetsCrawler
    {
        private static AzureClient azureClient = new AzureClient();
        private static StorageAccountProvider storageProvider = new StorageAccountProvider();

        [FunctionName("crawlavailabilitysets")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "crawlavailabilitysets")]HttpRequestMessage req, TraceWriter log)
        {
            try
            {
                var availability_sets = azureClient.azure.AvailabilitySets.List();
                TableBatchOperation availabilitySetBatchOperation = new TableBatchOperation();
                TableBatchOperation virtualMachineBatchOperation = new TableBatchOperation();
                foreach (var availabilitySet in availability_sets)
                {
                    AvailabilitySetsCrawlerResponseEntity availabilitySetsCrawlerResponseEntity = new AvailabilitySetsCrawlerResponseEntity("CrawlAS", Guid.NewGuid().ToString());
                    try
                    {
                        availabilitySetsCrawlerResponseEntity.EntryInsertionTime = DateTime.Now;
                        availabilitySetsCrawlerResponseEntity.Id = availabilitySet.Id;
                        availabilitySetsCrawlerResponseEntity.RegionName = availabilitySet.RegionName;
                        availabilitySetsCrawlerResponseEntity.ResourceGroupName = availabilitySet.Name;
                        availabilitySetsCrawlerResponseEntity.FaultDomainCount = availabilitySet.FaultDomainCount;
                        availabilitySetsCrawlerResponseEntity.UpdateDomainCount = availabilitySet.UpdateDomainCount;
                        if (availabilitySet.Inner.VirtualMachines.Count != 0)
                        {
                            if (availabilitySet.Inner.VirtualMachines.Count > 0)
                            {
                                List<string> virtualMachinesSet = new List<string>();
                                foreach (var vm_in_as in availabilitySet.Inner.VirtualMachines)
                                {
                                    virtualMachinesSet.Add(vm_in_as.Id.Split('/')[8]);
                                }
                                availabilitySetsCrawlerResponseEntity.Virtualmachines = string.Join(",", virtualMachinesSet);
                            }
                        }
                        var virtualMachinesList = azureClient.azure.VirtualMachines.ListByResourceGroup(availabilitySet.ResourceGroupName
                            ).Where(x => x.AvailabilitySetId == availabilitySet.Id);
                        foreach (var virtualMachine in virtualMachinesList)
                        {
                            var virtualMachineEntity = VirtualMachineHelper.ConvertToVirtualMachineEntity(virtualMachine, availabilitySet.ResourceGroupName);
                            virtualMachineEntity.VirtualMachineGroup = VirtualMachineGroup.AvailabilitySets.ToString();
                            virtualMachineBatchOperation.Insert(virtualMachineEntity);
                        }

                        availabilitySetBatchOperation.Insert(availabilitySetsCrawlerResponseEntity);
                    }
                    catch (Exception ex)
                    {
                        availabilitySetsCrawlerResponseEntity.Error = ex.Message;
                        log.Error($"AvailabilitySet Crawler trigger function Throw the exception ", ex, "VMChaos");
                        return req.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
                    }
                }

                await Task.Factory.StartNew(() =>
                {
                    if (availabilitySetBatchOperation.Count > 0)
                    {
                        CloudTable availabilitySetTable = storageProvider.CreateOrGetTable(azureClient.AvailabilitySetCrawlerTableName);
                        availabilitySetTable.ExecuteBatchAsync(availabilitySetBatchOperation);
                    }
                    if (virtualMachineBatchOperation.Count > 0)
                    {
                        CloudTable virtualMachineTable = storageProvider.CreateOrGetTable(azureClient.VirtualMachineCrawlerTableName);
                        virtualMachineTable.ExecuteBatchAsync(virtualMachineBatchOperation);
                    }
                });
            }
            catch (Exception ex)
            {
                throw ex;
            }

            // Fetching the name from the path parameter in the request URL
            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}