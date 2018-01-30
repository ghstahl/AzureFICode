using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AzureChaos;
using AzureChaos.Entity;
using AzureChaos.Models;
using AzureChaos.Providers;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace VirtualMachineChaosExecuter
{
    public static class ResourceGroupCrawler
    {
        private static ADConfiguration config = new ADConfiguration();
        private static StorageAccountProvider storageProvider = new StorageAccountProvider();
        private static CloudTableClient tableClient = storageProvider.CreateStorageAccountIfNotExist(config).CreateCloudTableClient();
        private static CloudTable table = tableClient.GetTableReference("ResourceGroupTable");
        private static TableBatchOperation batchOperation = new TableBatchOperation();

        [FunctionName("ResourceGroupCrawler")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetResourceGroups")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            // parse query parameter
            string name = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "name", true) == 0)
                .Value;

            //// Get request body
            dynamic data = await req.Content.ReadAsAsync<object>();
            //var storageAccount = storageProvider.CreateStorageAccountIfNotExist(config);

            try
            {
                var azure_client = AzureClient.GetAzure(config);
                var resourceGroups = azure_client.ResourceGroups.List();                
                foreach (var resourceGroup in resourceGroups)
                {
                    ResourceGroupCrawlerResponseEntity resourceGroupCrawlerResponseEntity = new ResourceGroupCrawlerResponseEntity("CrawlRGs", Guid.NewGuid().ToString());
                    try
                    {
                        resourceGroupCrawlerResponseEntity.EntryInsertionTime = DateTime.Now;
                        //resourceGroupCrawlerResponseEntity.EventType = data?.Action;
                        resourceGroupCrawlerResponseEntity.Id = resourceGroup.Id;
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
            }
            catch (Exception ex)
            {
                log.Error($"ResourceGroup Crawler function Throwed the exception ", ex, "RGCrawler");
                return req.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);

            }
            // Set name to query string or body data
            name = name ?? data?.name;
            await Task.Factory.StartNew(() =>
             {
                 table.ExecuteBatch(batchOperation);
             });
            return name == null
            ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body")
            : req.CreateResponse(HttpStatusCode.OK, "Hello " + name);
        }
    }
}
