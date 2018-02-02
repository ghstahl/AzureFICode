using AzureChaos;
using AzureChaos.Entity;
using AzureChaos.Enums;
using AzureChaos.Models;
using AzureChaos.Providers;
using ChaosExecuter.Helper;
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
    public static class VirtualMachineScaleSetCrawler
    {
        private static ADConfiguration config = new ADConfiguration();
        private static StorageAccountProvider storageProvider = new StorageAccountProvider(config);
        private static CloudTableClient tableClient = storageProvider.tableClient;
        private static CloudTable scaleSetTable = storageProvider.CreateOrGetTable("ScaleSets");
        private static CloudTable vmTable = storageProvider.CreateOrGetTable("VirtualMachineTable");

        [FunctionName("VirtualMachineScaleSetCrawler")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "crawlscalesets")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("VirtualMachineScaleSetCrawler function processed a request.");
            try
            {
                var azure_client = AzureClient.GetAzure(config);
                var scaleSets = await azure_client.VirtualMachineScaleSets.ListByResourceGroupAsync(config.ResourceGroup);
                var scaleSetVms = new List<IVirtualMachineScaleSetVMs>();
                VMScaleSetCrawlerResponseEntity vmScaletSetEntity = null;
                TableBatchOperation scaleSetbatchOperation = new TableBatchOperation();
                TableBatchOperation vmbatchOperation = new TableBatchOperation();
                foreach (var item in scaleSets)
                {
                    // insert the scale set details into table
                    vmScaletSetEntity = new VMScaleSetCrawlerResponseEntity(config.ResourceGroup, Guid.NewGuid().ToString());
                    vmScaletSetEntity.ResourceName = item.Name;
                    vmScaletSetEntity.ResourceType = item.Type;
                    vmScaletSetEntity.EntryInsertionTime = DateTime.UtcNow;
                    vmScaletSetEntity.ResourceGroupName = item.ResourceGroupName;
                    vmScaletSetEntity.RegionName = item.RegionName;
                    vmScaletSetEntity.Id = item.Id;
                    scaleSetbatchOperation.Insert(vmScaletSetEntity);

                    foreach (var instance in item.VirtualMachines.List())
                    {
                        // insert the scale set vm instances to table
                        vmbatchOperation.Insert(VirtualMachineHelper.ConvertToVirtualMachineEntity(instance, item.ResourceGroupName, item.Id, VirtualMachineGroup.ScaleSets.ToString()));
                    }
                }

                await Task.Factory.StartNew(() =>
                {
                    scaleSetTable.ExecuteBatch(scaleSetbatchOperation);
                    vmTable.ExecuteBatch(vmbatchOperation);
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
