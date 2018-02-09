using AzureChaos.Entity;
using AzureChaos.Models;
using AzureChaos.Providers;
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
            log.Info($"timercrawlerresourcegroups executed at: {DateTime.Now}");
            TableBatchOperation batchOperation = new TableBatchOperation();
            var azureSettings = AzureClient.azureSettings;
            try
            {
                var resourceGroups = AzureClient.azure.ResourceGroups.List();
                foreach (var resourceGroup in resourceGroups)
                {
                    ResourceGroupCrawlerResponseEntity resourceGroupCrawlerResponseEntity = new ResourceGroupCrawlerResponseEntity("crawlrg", resourceGroup.Id.Replace("/", "-"));
                    try
                    {
                        resourceGroupCrawlerResponseEntity.EntryInsertionTime = DateTime.Now;
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
                CloudTable table = await StorageProvider.CreateOrGetTableAsync(storageAccount, azureSettings.ResourceGroupCrawlerTableName);
                await table.ExecuteBatchAsync(batchOperation);
            }
        }
    }
}