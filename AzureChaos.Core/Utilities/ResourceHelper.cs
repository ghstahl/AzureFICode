using AzureChaos.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using FluentCore = Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Microsoft.Rest.Azure.OData;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Auth;
using System.Linq;
using AzureChaos.Entity;

namespace AzureChaos.Core.Utilities
{
    public static class ResourceHelper
    {
        public async static Task<IEnumerable<GenericResourceInner>> GetResources(ADConfiguration config)
        {
            try
            {
                var resourceClient = AzureClient.GetResourceManagementClient(config);
                return await resourceClient.ResourceGroups.ListResourcesAsync(config.ResourceGroup);

                ///using from SDK to get all the resources under resource group synchronizely
                /*ODataQuery<GenericResourceFilterInner> dataQuery = null;
                 var result = FluentCore.Extensions.Synchronize(() => resourceClient.ResourceGroups
                  .ListResourcesAsync(config.ResourceGroup, dataQuery))
                          .AsContinuousCollection((nextLink) => FluentCore.Extensions.Synchronize(() => resourceClient.ResourceGroups.ListResourcesNextAsync(nextLink)));
                  return result;*/

                /// need to figure out get the next set of resources simultenously using asynchronously. 
                /// Currentlt the below code is showing the error, will need to investigate

                /* var result = await resourceClient.ResourceGroups.ListResourcesAsync(config.ResourceGroup)
                      .ContinueWith(async (nextpage) => await resourceClient.ResourceGroups.ListResourcesNextAsync(config.ResourceGroup));
                  return result.Result;*/
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>Get the schedules from the schdeule table</summary>
        /// <param name="storageAccountName">Storage account name</param>
        /// <param name="storageAccountKey">Storage account key</param>
        /// <param name="tableName">Schedule table name</param>
        /// <returns>Returns the collection of chaos schedule table entity.</returns>
        public static async Task<IEnumerable<ChaosScheduleTableEntity>> GetSchedules(string storageAccountName, string storageAccountKey, string tableName)
        {
            CloudStorageAccount account = new CloudStorageAccount(new StorageCredentials(storageAccountName, storageAccountKey), true);
            CloudTableClient tableClient = account.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference(tableName);

            if (table == null)
            {
                return null;
            }

            TableContinuationToken continuationToken = null;
            IEnumerable<ChaosScheduleTableEntity> results = null;
            do
            {
                TableQuery<ChaosScheduleTableEntity> query = new TableQuery<ChaosScheduleTableEntity>();
                var result = await table.ExecuteQuerySegmentedAsync(query, continuationToken);
                if (result != null)
                {
                    results = results.Concat(result.Results);
                }

                continuationToken = result.ContinuationToken;
            } while (continuationToken != null);

            return results;
        }

        public static void GetRandomResourceType()
        {

        }
        /// <summary>Creating the schedule table if we dont find anything.</summary>
        /// <param name="config">AD configuration file</param>
        public static void CreateScheduleTable(ADConfiguration config)
        {
            var azure = AzureClient.GetAzure(config);
            string rgName = config.ResourceGroup;
            string storageAccountName = SdkContext.RandomResourceName("st", 10);
            var storage = azure.StorageAccounts.Define(storageAccountName)
                .WithRegion(Region.USEast)
                .WithNewResourceGroup(rgName)
                .Create();

            var storageKeys = storage.GetKeys();
            string storageConnectionString = "DefaultEndpointsProtocol=https;"
                + "AccountName=" + storage.Name
                + ";AccountKey=" + storageKeys[0].Value
                + ";EndpointSuffix=core.windows.net";

            var account = CloudStorageAccount.Parse(storageConnectionString);
            var tableClient = account.CreateCloudTableClient();
            // Retrieve a reference to the table.
            CloudTable table = tableClient.GetTableReference("chaosschedule");

            // Create the table if it doesn't exist.
            table.CreateIfNotExistsAsync();
        }
    }
}