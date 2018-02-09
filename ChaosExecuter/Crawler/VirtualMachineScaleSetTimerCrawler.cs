using AzureChaos.Entity;
using AzureChaos.Enums;
using AzureChaos.Helper;
using AzureChaos.Models;
using AzureChaos.Providers;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;

namespace ChaosExecuter.Crawler
{
    public static class VirtualMachineScaleSetTimerCrawler
    {
        private static AzureClient azureClient = new AzureClient();
        private static IStorageAccountProvider storageProvider = new StorageAccountProvider();

        [FunctionName("timercrawlerforvirtualmachinescaleset")]
        public static async void Run([TimerTrigger("0 */15 * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"timercrawlerforvirtualmachinescaleset executed at: {DateTime.Now}");
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
                    try
                    {
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
                    catch (Exception ex)
                    {
                        vmScaletSetEntity.Error = ex.Message;
                        log.Error($"timercrawlerforvirtualmachinescaleset threw the exception ", ex, "timercrawlerforvirtualmachinescaleset");
                    }
                }

                var storageAccount = storageProvider.CreateOrGetStorageAccount(azureClient);
                if (scaleSetbatchOperation.Count > 0)
                {
                    CloudTable scaleSetTable = await storageProvider.CreateOrGetTableAsync(storageAccount, azureClient.ScaleSetCrawlerTableName);
                    await scaleSetTable.ExecuteBatchAsync(scaleSetbatchOperation);
                }

                if (vmbatchOperation.Count > 0)
                {
                    CloudTable vmTable = await storageProvider.CreateOrGetTableAsync(storageAccount, azureClient.VirtualMachineCrawlerTableName);
                    await vmTable.ExecuteBatchAsync(vmbatchOperation);
                }
            }
            catch (Exception ex)
            {
                log.Error($"timercrawlerforvirtualmachinescaleset threw the exception ", ex, "timercrawlerforvirtualmachinescaleset");
            }
        }
    }
}
