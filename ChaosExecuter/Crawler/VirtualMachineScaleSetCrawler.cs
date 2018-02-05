using AzureChaos.Entity;
using AzureChaos.Enums;
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
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace ChaosExecuter.Crawler
{
    /// <summary>Crawler for virtual machine scale set.</summary>
    public static class VirtualMachineScaleSetCrawler
    {
        private static AzureClient azureClient = new AzureClient();
        private static StorageAccountProvider storageProvider = new StorageAccountProvider();

        /// <summary>Crawl the virtual machine scale sets and scale set vm instance for all resource group.</summary>
        /// <param name="req">The Http request.</param>
        /// <param name="log">The trace logger instance.</param>
        /// <returns></returns>
        [FunctionName("crawlscalesets")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "crawlscalesets")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("VirtualMachineScaleSetCrawler function processed a request.");
            try
            {
                TableBatchOperation scaleSetbatchOperation = new TableBatchOperation();
                TableBatchOperation vmbatchOperation = new TableBatchOperation();
                List<string> resourceGroupList = ResourceGroupHelper.GetResourceGroupsInSubscription(azureClient.azure);
                foreach (string resourceGroup in resourceGroupList)
                {
                    var scaleSets = await azureClient.azure.VirtualMachineScaleSets.ListByResourceGroupAsync(resourceGroup);
                    var scaleSetVms = new List<IVirtualMachineScaleSetVMs>();
                    VMScaleSetCrawlerResponseEntity vmScaletSetEntity = null;

                    foreach (var item in scaleSets)
                    {
                        // insert the scale set details into table
                        vmScaletSetEntity = new VMScaleSetCrawlerResponseEntity(azureClient.ResourceGroup, Guid.NewGuid().ToString());
                        vmScaletSetEntity.ResourceName = item.Name;
                        vmScaletSetEntity.ResourceType = item.Type;
                        vmScaletSetEntity.EntryInsertionTime = DateTime.UtcNow;
                        vmScaletSetEntity.ResourceGroupName = item.ResourceGroupName;
                        vmScaletSetEntity.RegionName = item.RegionName;
                        vmScaletSetEntity.Id = item.Id;
                        scaleSetbatchOperation.Insert(vmScaletSetEntity);
                        var virtualMachines = await item.VirtualMachines.ListAsync();
                        foreach (var instance in virtualMachines)
                        {
                            // insert the scale set vm instances to table
                            vmbatchOperation.Insert(VirtualMachineHelper.ConvertToVirtualMachineEntity(instance, item.ResourceGroupName, item.Id, VirtualMachineGroup.ScaleSets.ToString()));
                        }
                    }
                }

                await Task.Factory.StartNew(() =>
                {
                    if (scaleSetbatchOperation.Count > 0 && vmbatchOperation.Count > 0)
                    {
                        CloudTable scaleSetTable = storageProvider.CreateOrGetTable(azureClient.ScaleSetCrawlerTableName);
                        CloudTable vmTable = storageProvider.CreateOrGetTable(azureClient.VirtualMachineCrawlerTableName);
                        scaleSetTable.ExecuteBatch(scaleSetbatchOperation);
                        vmTable.ExecuteBatch(vmbatchOperation);
                    }
                });
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
