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
    /// <summary>Crawler for virtual machine scale set.</summary>
    public static class VirtualMachineScaleSetCrawler
    {
        private static readonly AzureClient AzureClient = new AzureClient();
        private static readonly IStorageAccountProvider StorageProvider = new StorageAccountProvider();

        /// <summary>Crawl the virtual machine scale sets and vm's in scale set for all resource group.</summary>
        /// <param name="req">The Azue Function Http request.</param>
        /// <param name="log">Logs all the traces in the Azue Function.</param>

        [FunctionName("crawlscalesets")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "crawlscalesets")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("VirtualMachineScaleSetCrawler function processed a request.");
            try
            {
                TableBatchOperation scaleSetbatchOperation = new TableBatchOperation();
                TableBatchOperation vmBatchOperation = new TableBatchOperation();
                List<string> resourceGroupList = ResourceGroupHelper.GetResourceGroupsInSubscription(AzureClient.azure);
                foreach (string resourceGroup in resourceGroupList)
                {
                    var scaleSetsList = await AzureClient.azure.VirtualMachineScaleSets.ListByResourceGroupAsync(resourceGroup);
                    //var scaleSetVms = new List<IVirtualMachineScaleSetVMs>();
                    VMScaleSetCrawlerResponseEntity vmScaletSetEntity = null;

                    foreach (var scaleSet in scaleSetsList)
                    {
                        try
                        {
                            // insert the scale set details into table
                            vmScaletSetEntity = new VMScaleSetCrawlerResponseEntity(AzureClient.ResourceGroup, scaleSet.Id.Replace("/", "-"))
                            {
                                ResourceName = scaleSet.Name,
                                ResourceType = scaleSet.Type,
                                EntryInsertionTime = DateTime.UtcNow,
                                ResourceGroupName = scaleSet.ResourceGroupName,
                                RegionName = scaleSet.RegionName,
                                Id = scaleSet.Id
                            };
                            if (scaleSet.AvailabilityZones.Count > 0)
                            {
                                vmScaletSetEntity.AvailabilityZone =
                                    int.Parse(scaleSet.AvailabilityZones.FirstOrDefault().Value);
                            }

                            scaleSetbatchOperation.InsertOrReplace(vmScaletSetEntity);
                            var virtualMachines = await scaleSet.VirtualMachines.ListAsync();
                            foreach (var instance in virtualMachines)
                            {
                                // insert the scale set vm instances to table
                                vmBatchOperation.InsertOrReplace(VirtualMachineHelper.ConvertToVirtualMachineEntity(instance,
                                    scaleSet.ResourceGroupName, scaleSet.Id, vmScaletSetEntity.AvailabilityZone,
                                    VirtualMachineGroup.ScaleSets.ToString()));
                            }
                        }
                        catch (Exception ex)
                        {
                            vmScaletSetEntity = new VMScaleSetCrawlerResponseEntity(AzureClient.ResourceGroup, scaleSet.Id.Replace("/", "-"))
                            {
                                Error = ex.Message
                            };
                            log.Error($"timercrawlerforvirtualmachinescaleset threw the exception ", ex, "timercrawlerforvirtualmachinescaleset");
                        }
                    }
                }

                var storageAccount = StorageProvider.CreateOrGetStorageAccount(AzureClient);
                if (scaleSetbatchOperation.Count > 0)
                {
                    CloudTable scaleSetTable = await StorageProvider.CreateOrGetTableAsync(storageAccount, AzureClient.ScaleSetCrawlerTableName);
                    await scaleSetTable.ExecuteBatchAsync(scaleSetbatchOperation);
                }

                if (vmBatchOperation.Count > 0)
                {
                    CloudTable vmTable = await StorageProvider.CreateOrGetTableAsync(storageAccount, AzureClient.VirtualMachineCrawlerTableName);
                    await vmTable.ExecuteBatchAsync(vmBatchOperation);
                }
            }
            catch (Exception ex)
            {
                log.Error($"VirtualMachineScaleSet Crawler function Throw the exception ", ex, "VirtualMachineScaleSetCrawler");
                return req.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}