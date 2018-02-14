using AzureChaos.Core.Entity;
using AzureChaos.Core.Models;
using AzureChaos.Core.Providers;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace ChaosExecuter.Crawler
{
    public static class ResourceGroupTimerCrawler
    {
        private static readonly AzureClient AzureClient = new AzureClient();
        private static readonly IStorageAccountProvider StorageProvider = new StorageAccountProvider();

        [FunctionName("timercrawlerresourcegroups")]
        public static async void Run([TimerTrigger("0 */15 * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"timercrawlerresourcegroups executed at: {DateTime.UtcNow}");
            var batchOperation = new TableBatchOperation();
            var azureSettings = AzureClient.AzureSettings;
            try
            {
                var resourceGroups = AzureClient.AzureInstance.ResourceGroups.List();
                foreach (var resourceGroup in resourceGroups)
                {
                    var resourceGroupCrawlerResponseEntity = new ResourceGroupCrawlerResponse("crawlrg", resourceGroup.Id.Replace("/", "-"));
                    try
                    {
                        resourceGroupCrawlerResponseEntity.EntryInsertionTime = DateTime.UtcNow;
                        resourceGroupCrawlerResponseEntity.ResourceGroupId = resourceGroup.Id;
                        resourceGroupCrawlerResponseEntity.RegionName = resourceGroup.RegionName;
                        resourceGroupCrawlerResponseEntity.ResourceGroupName = resourceGroup.Name;
                        batchOperation.InsertOrReplace(resourceGroupCrawlerResponseEntity);
                    }
                    catch (Exception ex)
                    {
                        resourceGroupCrawlerResponseEntity.Error = ex.Message;
                        log.Error($"timercrawlerresourcegroups threw exception ", ex, "timercrawlerresourcegroups");
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"timercrawlerresourcegroups threw exception ", ex, "timercrawlerresourcegroups");
            }

            var storageAccount = StorageProvider.CreateOrGetStorageAccount(AzureClient);
            if (batchOperation.Count > 0)
            {
                var table = await StorageProvider.CreateOrGetTableAsync(storageAccount, azureSettings.ResourceGroupCrawlerTableName);
                await table.ExecuteBatchAsync(batchOperation);
            }
        }
    }
}