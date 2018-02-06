using AzureChaos.Entity;
using AzureChaos.Models;
using AzureChaos.Providers;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Threading.Tasks;

namespace ChaosExecuter.Crawler
{
    public static class ResourceGroupTimerCrawler
    {
        private static AzureClient azureClient = new AzureClient();
        private static StorageAccountProvider storageProvider = new StorageAccountProvider(azureClient);

        [FunctionName("timercrawlerresourcegroups")]
        public static async void Run([TimerTrigger("0 */15 * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"timercrawlerresourcegroups executed at: {DateTime.Now}");
            TableBatchOperation batchOperation = new TableBatchOperation();
            try
            {
                var resourceGroups = azureClient.azure.ResourceGroups.List();
                foreach (var resourceGroup in resourceGroups)
                {
                    ResourceGroupCrawlerResponseEntity resourceGroupCrawlerResponseEntity = new ResourceGroupCrawlerResponseEntity("CrawlRGs", Guid.NewGuid().ToString());
                    try
                    {
                        resourceGroupCrawlerResponseEntity.EntryInsertionTime = DateTime.Now;
                        resourceGroupCrawlerResponseEntity.ResourceGroupId = resourceGroup.Id;
                        resourceGroupCrawlerResponseEntity.RegionName = resourceGroup.RegionName;
                        resourceGroupCrawlerResponseEntity.ResourceGroupName = resourceGroup.Name;
                        batchOperation.Insert(resourceGroupCrawlerResponseEntity);
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

            if (batchOperation.Count > 0)
            {
                CloudTable table = await storageProvider.CreateOrGetTable(azureClient.ResourceGroupCrawlerTableName);
                await table.ExecuteBatchAsync(batchOperation);
            }
        }
    }
}
