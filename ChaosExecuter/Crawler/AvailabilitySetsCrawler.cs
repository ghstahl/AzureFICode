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
                var azureSettings = AzureClient.azureSettings;
                List<string> resourceGroupList = ResourceGroupHelper.GetResourceGroupsInSubscription(AzureClient.azure);
                var storageAccount = StorageProvider.CreateOrGetStorageAccount(AzureClient);
                foreach (string resourceGroup in resourceGroupList)
                {
                    var availability_sets = await AzureClient.azure.AvailabilitySets.ListByResourceGroupAsync(resourceGroup);
                    TableBatchOperation availabilitySetBatchOperation = new TableBatchOperation();
                    TableBatchOperation virtualMachineBatchOperation = new TableBatchOperation();
                    foreach (var availabilitySet in availability_sets)
                    {
                        AvailabilitySetsCrawlerResponseEntity availabilitySetsCrawlerResponseEntity = new AvailabilitySetsCrawlerResponseEntity(availabilitySet.ResourceGroupName, availabilitySet.Name);
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
                                var partitionKey = availabilitySet.Id.Replace('/', '!');
                                foreach (var virtualMachine in vmList)
                                {
                                    virtualMachinesSet.Add(virtualMachine.Name);
                                    var virtualMachineEntity = VirtualMachineHelper.ConvertToVirtualMachineEntity(virtualMachine, partitionKey, VirtualMachineGroup.AvailabilitySets.ToString());
                                    virtualMachineBatchOperation.InsertOrReplace(virtualMachineEntity);
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

                    if (availabilitySetBatchOperation.Count > 0)
                    {
                        CloudTable availabilitySetTable = await StorageProvider.CreateOrGetTableAsync(storageAccount, azureSettings.AvailabilitySetCrawlerTableName);
                        await availabilitySetTable.ExecuteBatchAsync(availabilitySetBatchOperation);
                    }

                    if (virtualMachineBatchOperation.Count > 0)
                    {
                        CloudTable virtualMachineTable = await StorageProvider.CreateOrGetTableAsync(storageAccount, azureSettings.VirtualMachineCrawlerTableName);
                        await virtualMachineTable.ExecuteBatchAsync(virtualMachineBatchOperation);
                    }
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