using AzureChaos.Entity;
using AzureChaos.Enums;
using AzureChaos.Helper;
using AzureChaos.Models;
using AzureChaos.Providers;
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

            var azureSettings = AzureClient.azureSettings;
            var resourceGroupList = ResourceGroupHelper.GetResourceGroupsInSubscription(AzureClient.azure, azureSettings);
            if (resourceGroupList == null)
            {
                log.Info($"timercrawlerforavailabilitysets: no resource groups to crawler");
                return;
            }

            var storageAccount = StorageProvider.CreateOrGetStorageAccount(AzureClient);
            foreach (var resourceGroup in resourceGroupList)
            {
                var availability_sets = await AzureClient.azure.AvailabilitySets.ListByResourceGroupAsync(resourceGroup.Name);
                TableBatchOperation availabilitySetBatchOperation = new TableBatchOperation();
                foreach (var availabilitySet in availability_sets)
                {
                    AvailabilitySetsCrawlerResponseEntity availabilitySetsCrawlerResponseEntity = new AvailabilitySetsCrawlerResponseEntity(availabilitySet.ResourceGroupName, availabilitySet.Id.Replace("/", "!"));
                    try
                    {
                        availabilitySetsCrawlerResponseEntity.EntryInsertionTime = DateTime.UtcNow;
                        availabilitySetsCrawlerResponseEntity.Id = availabilitySet.Id;
                        availabilitySetsCrawlerResponseEntity.RegionName = availabilitySet.RegionName;
                        availabilitySetsCrawlerResponseEntity.ResourceGroupName = availabilitySet.Name;
                        availabilitySetsCrawlerResponseEntity.FaultDomainCount = availabilitySet.FaultDomainCount;
                        availabilitySetsCrawlerResponseEntity.UpdateDomainCount = availabilitySet.UpdateDomainCount;
                        if (availabilitySet.Inner == null || availabilitySet.Inner.VirtualMachines == null || availabilitySet.Inner.VirtualMachines.Count == 0)
                        {
                            availabilitySetBatchOperation.InsertOrReplace(availabilitySetsCrawlerResponseEntity);
                            continue;
                        }

                        if (availabilitySet.Inner.VirtualMachines.Count > 0)
                        {
                            List<string> virtualMachinesSet = new List<string>();
                            foreach (var vm_in_as in availabilitySet.Inner.VirtualMachines)
                            {
                                virtualMachinesSet.Add(vm_in_as.Id.Split('/')[8]);
                            }

                            availabilitySetsCrawlerResponseEntity.Virtualmachines = string.Join(",", virtualMachinesSet);
                        }

                        availabilitySetBatchOperation.InsertOrReplace(availabilitySetsCrawlerResponseEntity);
                        var pageCollection = await AzureClient.azure.VirtualMachines.ListByResourceGroupAsync(availabilitySet.ResourceGroupName);
                        if (pageCollection == null)
                        {
                            continue;
                        }

                        var virtualMachinesList = pageCollection.Where(x => availabilitySet.Id.Equals(x.AvailabilitySetId, StringComparison.OrdinalIgnoreCase));
                        var partitionKey = availabilitySet.Id.Replace('/', '!');
                        TableBatchOperation virtualMachineBatchOperation = new TableBatchOperation();
                        foreach (var virtualMachine in virtualMachinesList)
                        {
                            var virtualMachineEntity = VirtualMachineHelper.ConvertToVirtualMachineEntity(virtualMachine, partitionKey, availabilitySet.ResourceGroupName);
                            virtualMachineEntity.VirtualMachineGroup = VirtualMachineGroup.AvailabilitySets.ToString();
                            virtualMachineBatchOperation.InsertOrReplace(virtualMachineEntity);
                        }

                        if (virtualMachineBatchOperation.Count > 0)
                        {
                            CloudTable virtualMachineTable = await StorageProvider.CreateOrGetTableAsync(storageAccount, azureSettings.VirtualMachineCrawlerTableName);
                            await virtualMachineTable.ExecuteBatchAsync(virtualMachineBatchOperation);
                        }

                    }
                    catch (Exception ex)
                    {
                        availabilitySetsCrawlerResponseEntity.Error = ex.Message;
                        log.Error($"timercrawlerforavailabilitysets threw the exception ", ex, "timercrawlerforavailabilitysets");
                    }
                }

                if (availabilitySetBatchOperation.Count > 0)
                {
                    CloudTable availabilitySetTable = await StorageProvider.CreateOrGetTableAsync(storageAccount, azureSettings.AvailabilitySetCrawlerTableName);
                    await availabilitySetTable.ExecuteBatchAsync(availabilitySetBatchOperation);
                }
            }
        }
    }
}