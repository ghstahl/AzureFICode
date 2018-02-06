using AzureChaos.Entity;
using AzureChaos.Models;
using AzureChaos.Providers;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace ChaosExecuter.Crawler
{
    public static class ResourceGroupCrawler
    {
        private static AzureClient azureClient = new AzureClient();
        private static StorageAccountProvider storageProvider = new StorageAccountProvider(azureClient);

        [FunctionName("crawlresourcesgroups")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "crawlresourcesgroups")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");
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
                        log.Error($"VM Chaos trigger function Throw the exception ", ex, "VMChaos");
                        return req.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
                    }
                }

                if (batchOperation.Count > 0)
                {
                    CloudTable table = await storageProvider.CreateOrGetTable(azureClient.ResourceGroupCrawlerTableName);
                    await table.ExecuteBatchAsync(batchOperation);
                }
            }
            catch (Exception ex)
            {
                log.Error($"ResourceGroup Crawler function Throwed the exception ", ex, "RGCrawler");
                return req.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }



            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}
