using AzureChaos.Core.Entity;
using AzureChaos.Core.Enums;
using AzureChaos.Core.Helper;
using AzureChaos.Core.Models;
using AzureChaos.Core.Providers;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace ChaosExecuter.Crawler
{
    public static class VirtualMachineScaleSetTimerCrawler
    {
        private static readonly AzureClient AzureClient = new AzureClient();
        private static readonly IStorageAccountProvider StorageProvider = new StorageAccountProvider();

        [FunctionName("timercrawlerforvirtualmachinescaleset")]
        public static async void Run([TimerTrigger("0 */15 * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"timercrawlerforvirtualmachinescaleset executed at: {DateTime.UtcNow}");

            var azureSettings = AzureClient.AzureSettings;
            var resourceGroupList = ResourceGroupHelper.GetResourceGroupsInSubscription(AzureClient.AzureInstance, azureSettings);
            if (resourceGroupList == null)
            {
                log.Info($"timercrawlerforvirtualmachinescaleset: no resource groups to crawl");
                return;
            }

            var storageAccount = StorageProvider.CreateOrGetStorageAccount(AzureClient);
            foreach (var resourceGroup in resourceGroupList)
            {
                var scaleSetsList = await AzureClient.AzureInstance.VirtualMachineScaleSets.ListByResourceGroupAsync(resourceGroup.Name);
                try
                {
                    var scaleSetbatchOperation = new TableBatchOperation();
                    foreach (var scaleSet in scaleSetsList)
                    {
                        // insert the scale set details into table
                        var vmScaletSetEntity = new VmScaleSetCrawlerResponse(scaleSet.ResourceGroupName, scaleSet.Id.Replace("/", "!"))
                        {
                            ResourceName = scaleSet.Name,
                            ResourceType = scaleSet.Type,
                            EntryInsertionTime = DateTime.UtcNow,
                            ResourceGroupName = scaleSet.ResourceGroupName,
                            RegionName = scaleSet.RegionName,
                            Id = scaleSet.Id
                        };
                        var virtualMachines = await scaleSet.VirtualMachines.ListAsync();
                        if(virtualMachines == null)
                        {
                            scaleSetbatchOperation.InsertOrReplace(vmScaletSetEntity);
                            continue;
                        }

                        vmScaletSetEntity.HasVirtualMachines = true;
                        scaleSetbatchOperation.InsertOrReplace(vmScaletSetEntity);

                        var partitionKey = scaleSet.Id.Replace('/', '!');
                        var vmbatchOperation = new TableBatchOperation();
                        foreach (var instance in virtualMachines)
                        {
                            // insert the scale set vm instances to table
                            vmbatchOperation.InsertOrReplace(VirtualMachineHelper.ConvertToVirtualMachineEntity(instance, scaleSet.ResourceGroupName, scaleSet.Id, partitionKey, vmScaletSetEntity.AvailabilityZone, VirtualMachineGroup.ScaleSets.ToString()));
                        }

                        if (vmbatchOperation.Count <= 0) continue;
                        var vmTable = await StorageProvider.CreateOrGetTableAsync(storageAccount, azureSettings.VirtualMachineCrawlerTableName);
                        await vmTable.ExecuteBatchAsync(vmbatchOperation);
                    }

                    if (scaleSetbatchOperation.Count > 0)
                    {
                        CloudTable scaleSetTable = await StorageProvider.CreateOrGetTableAsync(storageAccount, azureSettings.ScaleSetCrawlerTableName);
                        await scaleSetTable.ExecuteBatchAsync(scaleSetbatchOperation);
                    }
                }
                catch (Exception ex)
                {
                    log.Error($"timercrawlerforvirtualmachinescaleset threw the exception ", ex, "timercrawlerforvirtualmachinescaleset");
                }
            }

        }

    }
}
