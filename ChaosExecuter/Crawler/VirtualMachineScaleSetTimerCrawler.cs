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
using System.Linq;

namespace ChaosExecuter.Crawler
{
    public static class VirtualMachineScaleSetTimerCrawler
    {
        private static readonly AzureClient AzureClient = new AzureClient();
        private static readonly IStorageAccountProvider StorageProvider = new StorageAccountProvider();

        [FunctionName("timercrawlerforvirtualmachinescaleset")]
        public static async void Run([TimerTrigger("0 */15 * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"timercrawlerforvirtualmachinescaleset executed at: {DateTime.Now}");
            try
            {
                TableBatchOperation scaleSetbatchOperation = new TableBatchOperation();
                TableBatchOperation vmbatchOperation = new TableBatchOperation();
                List<string> resourceGroupList = ResourceGroupHelper.GetResourceGroupsInSubscription(AzureClient.azure);
                foreach (string resourceGroup in resourceGroupList)
                {
                    var scaleSetsList = await AzureClient.azure.VirtualMachineScaleSets.ListByResourceGroupAsync(resourceGroup);
                    var scaleSetVms = new List<IVirtualMachineScaleSetVMs>();
                    VMScaleSetCrawlerResponseEntity vmScaletSetEntity = null;
                    try
                    {
                        foreach (var scaleSet in scaleSetsList)
                        {
                            // insert the scale set details into table
                            vmScaletSetEntity = new VMScaleSetCrawlerResponseEntity(AzureClient.ResourceGroup, Guid.NewGuid().ToString())
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
                                vmScaletSetEntity.AvailabilityZone = int.Parse(scaleSet.AvailabilityZones.FirstOrDefault().Value);
                            }
                            scaleSetbatchOperation.Insert(vmScaletSetEntity);
                            var virtualMachines = await scaleSet.VirtualMachines.ListAsync();
                            foreach (var instance in virtualMachines)
                            {
                                // insert the scale set vm instances to table
                                vmbatchOperation.Insert(VirtualMachineHelper.ConvertToVirtualMachineEntity(instance, scaleSet.ResourceGroupName, scaleSet.Id, vmScaletSetEntity.AvailabilityZone, VirtualMachineGroup.ScaleSets.ToString()));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        vmScaletSetEntity = new VMScaleSetCrawlerResponseEntity(AzureClient.ResourceGroup, Guid.NewGuid().ToString())
                        {
                            Error = ex.Message
                        };
                        log.Error($"timercrawlerforvirtualmachinescaleset threw the exception ", ex, "timercrawlerforvirtualmachinescaleset");
                    }
                }

                var storageAccount = StorageProvider.CreateOrGetStorageAccount(AzureClient);
                if (scaleSetbatchOperation.Count > 0)
                {
                    CloudTable scaleSetTable = await StorageProvider.CreateOrGetTableAsync(storageAccount, AzureClient.ScaleSetCrawlerTableName);
                    await scaleSetTable.ExecuteBatchAsync(scaleSetbatchOperation);
                }

                if (vmbatchOperation.Count > 0)
                {
                    CloudTable vmTable = await StorageProvider.CreateOrGetTableAsync(storageAccount, AzureClient.VirtualMachineCrawlerTableName);
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