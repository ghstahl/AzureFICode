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
        private static readonly AzureClient AzureClient = new AzureClient();
        private static readonly IStorageAccountProvider StorageProvider = new StorageAccountProvider();

        [FunctionName("crawlavailabilitysets")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "crawlavailabilitysets")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("AvalibilitySetsCrawler function processed a request.");
            try
            {
                var availabilitySetsets = AzureClient.azure.AvailabilitySets.List();
                TableBatchOperation availabilitySetBatchOperation = new TableBatchOperation();
                TableBatchOperation virtualMachineBatchOperation = new TableBatchOperation();
                foreach (var availabilitySet in availabilitySetsets)
                {
                    AvailabilitySetsCrawlerResponseEntity availabilitySetsCrawlerResponseEntity = new AvailabilitySetsCrawlerResponseEntity("crawlas", availabilitySet.Id.Replace("/", "-"));
                    try
                    {
                        availabilitySetsCrawlerResponseEntity.EntryInsertionTime = DateTime.Now;
                        availabilitySetsCrawlerResponseEntity.Id = availabilitySet.Id;
                        availabilitySetsCrawlerResponseEntity.RegionName = availabilitySet.RegionName;
                        availabilitySetsCrawlerResponseEntity.ResourceGroupName = availabilitySet.Name;
                        availabilitySetsCrawlerResponseEntity.ResourceType = availabilitySet.Type;
                        availabilitySetsCrawlerResponseEntity.FaultDomainCount = availabilitySet.FaultDomainCount;
                        availabilitySetsCrawlerResponseEntity.UpdateDomainCount = availabilitySet.UpdateDomainCount;
                        var pagedCollection = await AzureClient.azure.VirtualMachines.ListByResourceGroupAsync(availabilitySet.ResourceGroupName);
                        if (pagedCollection != null && pagedCollection.Any())
                        {
                            var vmList = pagedCollection.Where(x => availabilitySet.Id.Equals(x.AvailabilitySetId, StringComparison.OrdinalIgnoreCase));
                            List<string> virtualMachinesSet = new List<string>();
                            foreach (var virtualMachine in vmList)
                            {
                                virtualMachinesSet.Add(virtualMachine.Name);
                                var virtualMachineEntity = VirtualMachineHelper.ConvertToVirtualMachineEntity(virtualMachine, VirtualMachineGroup.AvailabilitySets.ToString());
                                virtualMachineBatchOperation.Insert(virtualMachineEntity);
                            }

                            availabilitySetsCrawlerResponseEntity.Virtualmachines = string.Join(",", virtualMachinesSet);
                        }

                        availabilitySetBatchOperation.InsertOrReplace(availabilitySetsCrawlerResponseEntity);
                    }
                    catch (Exception ex)
                    {
                        availabilitySetsCrawlerResponseEntity.Error = ex.Message;
                        log.Error($"AvailabilitySet Crawler trigger function Throw the exception ", ex, "VMChaos");
                        return req.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
                    }
                }

                var storageAccount = StorageProvider.CreateOrGetStorageAccount(AzureClient);
                if (availabilitySetBatchOperation.Count > 0)
                {
                    CloudTable availabilitySetTable = await StorageProvider.CreateOrGetTableAsync(storageAccount, AzureClient.AvailabilitySetCrawlerTableName);
                    await availabilitySetTable.ExecuteBatchAsync(availabilitySetBatchOperation);
                }

                if (virtualMachineBatchOperation.Count > 0)
                {
                    CloudTable virtualMachineTable = await StorageProvider.CreateOrGetTableAsync(storageAccount, AzureClient.VirtualMachineCrawlerTableName);
                    await virtualMachineTable.ExecuteBatchAsync(virtualMachineBatchOperation);
                }
            }
            catch (Exception ex)
            {
                log.Error($"AvailabilitySet Crawler trigger function Throw the exception ", ex, "VMChaos");
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            // Fetching the name from the path parameter in the request URL
            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}