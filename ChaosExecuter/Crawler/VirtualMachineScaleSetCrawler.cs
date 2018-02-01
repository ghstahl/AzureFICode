using AzureChaos;
using AzureChaos.Entity;
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
    public static class VirtualMachineScaleSetCrawler
    {
        private static ADConfiguration config = new ADConfiguration();
        private static StorageAccountProvider storageProvider = new StorageAccountProvider(config);
        private static CloudTableClient tableClient = storageProvider.tableClient;
        private static CloudTable table = storageProvider.CreateOrGetTable("ScaleSets");

        [FunctionName("VirtualMachineScaleSetCrawler")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "GetVMScaleSets")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("VirtualMachineScaleSetCrawler function processed a request.");
            try
            {
                var azure_client = AzureClient.GetAzure(config);
                var scaleSets = azure_client.VirtualMachineScaleSets.List();
                var scaleSetVms = new List<IVirtualMachineScaleSetVMs>();
                VMScaleSetCrawlerResponseEntity vmScaletSetEntity = null;
                TableBatchOperation batchOperation = new TableBatchOperation();
                foreach (var item in scaleSets)
                {
                    // insert the scale set details into table
                    vmScaletSetEntity = new VMScaleSetCrawlerResponseEntity(config.ResourceGroup, Guid.NewGuid().ToString());
                    vmScaletSetEntity.ResourceName = item.Name;
                    vmScaletSetEntity.ResourceType = item.Type;
                    vmScaletSetEntity.EntryInsertionTime = DateTime.UtcNow;
                    vmScaletSetEntity.ResourceGroupName = item.ResourceGroupName;
                    vmScaletSetEntity.RegionName = item.RegionName;
                    batchOperation.Insert(vmScaletSetEntity);

                    foreach (var instance in item.VirtualMachines.List())
                    {
                        // insert the scale set vm instances to table
                        vmScaletSetEntity = new VMScaleSetCrawlerResponseEntity(config.ResourceGroup, Guid.NewGuid().ToString());
                        vmScaletSetEntity.ResourceGroupName = item.ResourceGroupName;
                        vmScaletSetEntity.ResourceName = instance.Name;
                        vmScaletSetEntity.RegionName = item.RegionName;
                        vmScaletSetEntity.ResourceType = instance.Type;
                        vmScaletSetEntity.EntryInsertionTime = DateTime.UtcNow;
                        batchOperation.Insert(vmScaletSetEntity);
                    }
                }

                await Task.Factory.StartNew(() =>
                {
                    table.ExecuteBatch(batchOperation);
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
