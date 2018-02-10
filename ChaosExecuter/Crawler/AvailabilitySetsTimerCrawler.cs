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
        public static async void Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"timercrawlerforavailabilitysets executed at: {DateTime.Now}");

            var azureSettings = AzureClient.azureSettings;
            List<string> resourceGroupList = ResourceGroupHelper.GetResourceGroupsInSubscription(AzureClient.azure, azureSettings.Chaos.BlackListedResourceGroups);
            var storageAccount = StorageProvider.CreateOrGetStorageAccount(AzureClient);
            foreach (string resourceGroup in resourceGroupList)
            {
                var availability_sets = await AzureClient.azure.AvailabilitySets.ListByResourceGroupAsync(resourceGroup);
                TableBatchOperation availabilitySetBatchOperation = new TableBatchOperation();
                foreach (var availabilitySet in availability_sets)
                {
                    AvailabilitySetsCrawlerResponseEntity availabilitySetsCrawlerResponseEntity = new AvailabilitySetsCrawlerResponseEntity(availabilitySet.ResourceGroupName, availabilitySet.Name.ToString());
                    try
                    {
                        availabilitySetsCrawlerResponseEntity.EntryInsertionTime = DateTime.Now;
                        availabilitySetsCrawlerResponseEntity.Id = availabilitySet.Id;
                        availabilitySetsCrawlerResponseEntity.RegionName = availabilitySet.RegionName;
                        availabilitySetsCrawlerResponseEntity.ResourceGroupName = availabilitySet.Name;
                        availabilitySetsCrawlerResponseEntity.FaultDomainCount = availabilitySet.FaultDomainCount;
                        availabilitySetsCrawlerResponseEntity.UpdateDomainCount = availabilitySet.UpdateDomainCount;
                        if (availabilitySet.Inner.VirtualMachines.Count != 0)
                        {
                            if (availabilitySet.Inner.VirtualMachines.Count > 0)
                            {
                                List<string> virtualMachinesSet = new List<string>();
                                foreach (var vm_in_as in availabilitySet.Inner.VirtualMachines)
                                {
                                    virtualMachinesSet.Add(vm_in_as.Id.Split('/')[8]);
                                }
                                availabilitySetsCrawlerResponseEntity.Virtualmachines = string.Join(",", virtualMachinesSet);
                            }
                        }

                        var virtualMachinesList = AzureClient.azure.VirtualMachines.ListByResourceGroup(availabilitySet.ResourceGroupName
                            ).Where(x => x.AvailabilitySetId == availabilitySet.Id);
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


                        availabilitySetBatchOperation.InsertOrReplace(availabilitySetsCrawlerResponseEntity);
                    }
                    catch (Exception ex)
                    {
                        availabilitySetsCrawlerResponseEntity.Error = ex.Message;
                        log.Error($"timercrawlerforavailabilitysets threw the exception ", ex, "timercrawlerforavailabilitysets");
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
}