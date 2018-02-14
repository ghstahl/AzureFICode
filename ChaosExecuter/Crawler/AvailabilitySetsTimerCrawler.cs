using AzureChaos.Core.Entity;
using AzureChaos.Core.Enums;
using AzureChaos.Core.Helper;
using AzureChaos.Core.Models;
using AzureChaos.Core.Providers;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChaosExecuter.Crawler
{
    public static class AvailabilitySetsTimerCrawler
    {
        private static readonly AzureClient AzureClient = new AzureClient();
        private static readonly IStorageAccountProvider StorageProvider = new StorageAccountProvider();

        [FunctionName("timercrawlerforavailabilitysets")]
        public static async void Run([TimerTrigger("0 */15 * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"timercrawlerforavailabilitysets executed at: {DateTime.UtcNow}");

            var azureSettings = AzureClient.AzureSettings;
            var resourceGroupList = ResourceGroupHelper.GetResourceGroupsInSubscription(AzureClient.AzureInstance, azureSettings);
            if (resourceGroupList == null)
            {
                log.Info($"timercrawlerforavailabilitysets: no resource groups to crawler");
                return;
            }

            var storageAccount = StorageProvider.CreateOrGetStorageAccount(AzureClient);
            foreach (var resourceGroup in resourceGroupList)
            {
                var availabilitySets = await AzureClient.AzureInstance.AvailabilitySets.ListByResourceGroupAsync(resourceGroup.Name);
                TableBatchOperation availabilitySetBatchOperation = new TableBatchOperation();
                foreach (var availabilitySet in availabilitySets)
                {
                    var availabilitySetsCrawlerResponseEntity = new AvailabilitySetsCrawlerResponseEntity(availabilitySet.ResourceGroupName, availabilitySet.Id.Replace("/", "!"));
                    try
                    {
                        availabilitySetsCrawlerResponseEntity.EntryInsertionTime = DateTime.UtcNow;
                        availabilitySetsCrawlerResponseEntity.Id = availabilitySet.Id;
                        availabilitySetsCrawlerResponseEntity.RegionName = availabilitySet.RegionName;
                        availabilitySetsCrawlerResponseEntity.ResourceGroupName = availabilitySet.Name;
                        availabilitySetsCrawlerResponseEntity.FaultDomainCount = availabilitySet.FaultDomainCount;
                        availabilitySetsCrawlerResponseEntity.UpdateDomainCount = availabilitySet.UpdateDomainCount;
                        if (availabilitySet.Inner?.VirtualMachines == null || availabilitySet.Inner.VirtualMachines.Count == 0)
                        {
                            availabilitySetBatchOperation.InsertOrReplace(availabilitySetsCrawlerResponseEntity);
                            continue;
                        }

                        if (availabilitySet.Inner.VirtualMachines.Count > 0)
                        {
                            List<string> virtualMachinesSet = new List<string>();
                            foreach (var vmInAs in availabilitySet.Inner.VirtualMachines)
                            {
                                virtualMachinesSet.Add(vmInAs.Id.Split('/')[8]);
                            }

                            availabilitySetsCrawlerResponseEntity.Virtualmachines = string.Join(",", virtualMachinesSet);
                        }

                        availabilitySetBatchOperation.InsertOrReplace(availabilitySetsCrawlerResponseEntity);
                        var pageCollection = await AzureClient.AzureInstance.VirtualMachines.ListByResourceGroupAsync(availabilitySet.ResourceGroupName);
                        if (pageCollection == null)
                        {
                            continue;
                        }

                        var virtualMachinesList = pageCollection.Where(x => availabilitySet.Id.Equals(x.AvailabilitySetId, StringComparison.OrdinalIgnoreCase));
                        var partitionKey = availabilitySet.Id.Replace('/', '!');
                        TableBatchOperation virtualMachineBatchOperation = new TableBatchOperation();
                        foreach (var virtualMachine in virtualMachinesList)
                        {
                            var virtualMachineEntity = VirtualMachineHelper.ConvertToVirtualMachineEntity(
                                virtualMachine,
                                partitionKey,
                                availabilitySet.ResourceGroupName);
                            virtualMachineEntity.VirtualMachineGroup = VirtualMachineGroup.AvailabilitySets.ToString();
                            virtualMachineBatchOperation.InsertOrReplace(virtualMachineEntity);
                        }

                        if (virtualMachineBatchOperation.Count > 0)
                        {
                            var virtualMachineTable = await StorageProvider.CreateOrGetTableAsync(storageAccount,
                                azureSettings.VirtualMachineCrawlerTableName);
                            await virtualMachineTable.ExecuteBatchAsync(virtualMachineBatchOperation);
                        }

                    }
                    catch (Exception ex)
                    {
                        availabilitySetsCrawlerResponseEntity.Error = ex.Message;
                        log.Error($"timercrawlerforavailabilitysets threw the exception ", ex, "timercrawlerforavailabilitysets");
                    }
                }

                if (availabilitySetBatchOperation.Count <= 0) continue;
                var availabilitySetTable = await StorageProvider.CreateOrGetTableAsync(storageAccount, azureSettings.AvailabilitySetCrawlerTableName);
                await availabilitySetTable.ExecuteBatchAsync(availabilitySetBatchOperation);
            }
        }
    }
}