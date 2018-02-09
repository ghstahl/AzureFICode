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
            log.Info($"timercrawlerforavailabilitysets executed at: {DateTime.Now}");
            try
            {
                var availabilitySets = AzureClient.azure.AvailabilitySets.List();
                TableBatchOperation availabilitySetBatchOperation = new TableBatchOperation();
                TableBatchOperation virtualMachineBatchOperation = new TableBatchOperation();
                foreach (var availabilitySet in availabilitySets)
                {
                    AvailabilitySetsCrawlerResponseEntity availabilitySetsCrawlerResponseEntity = new AvailabilitySetsCrawlerResponseEntity("crawlas", availabilitySet.Id.Replace("/", "-"));
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
                                foreach (var virtuaMachine in availabilitySet.Inner.VirtualMachines)
                                {
                                    virtualMachinesSet.Add(virtuaMachine.Id.Split('/')[8]);
                                }
                                availabilitySetsCrawlerResponseEntity.Virtualmachines = string.Join(",", virtualMachinesSet);
                            }
                        }

                        var virtualMachinesList = AzureClient.azure.VirtualMachines.ListByResourceGroup(availabilitySet.ResourceGroupName
                            ).Where(x => x.AvailabilitySetId == availabilitySet.Id);
                        foreach (var virtualMachine in virtualMachinesList)
                        {
                            var virtualMachineEntity = VirtualMachineHelper.ConvertToVirtualMachineEntity(virtualMachine, availabilitySet.ResourceGroupName);
                            virtualMachineEntity.VirtualMachineGroup = VirtualMachineGroup.AvailabilitySets.ToString();
                            virtualMachineBatchOperation.InsertOrReplace(virtualMachineEntity);
                        }

                        availabilitySetBatchOperation.Insert(availabilitySetsCrawlerResponseEntity);
                    }
                    catch (Exception ex)
                    {
                        availabilitySetsCrawlerResponseEntity.Error = ex.Message;
                        log.Error($"timercrawlerforavailabilitysets threw the exception ", ex, "timercrawlerforavailabilitysets");
                    }
                }

                var storageAccount = StorageProvider.CreateOrGetStorageAccount(AzureClient);
                if (availabilitySetBatchOperation.Count > 0)
                {
                    CloudTable availabilitySetTable = await StorageProvider.CreateOrGetTableAsync(storageAccount, AzureClient.AvailabilitySetCrawlerTableName);
                    await availabilitySetTable.ExecuteBatchAsync(availabilitySetBatchOperation);
                }
                if (virtualMachineBatchOperation.Count > 0)
                {
                    CloudTable virtualMachineTable = await StorageProvider.CreateOrGetTableAsync(storageAccount, AzureClient.VirtualMachineCrawlerTableName);
                    await virtualMachineTable.ExecuteBatchAsync(virtualMachineBatchOperation);
                }
            }
            catch (Exception ex)
            {
                log.Error($"timercrawlerforavailabilitysets threw the exception ", ex, "timercrawlerforavailabilitysets");
                //throw ex;
            }
        }
    }
}