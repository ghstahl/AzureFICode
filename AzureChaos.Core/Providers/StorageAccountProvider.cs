using AzureChaos.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureChaos.Providers
{
    /// <summary>The storage account provider.</summary>
    /// Creates the storage account if not any for the given storage account name in the config.
    /// Create the table client for the given storage account.
    public class StorageAccountProvider : IStorageAccountProvider
    {
        /// <summary>Default format for the storage connection string.</summary>
        private const string connectionStringFormat = "DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1};EndpointSuffix=core.windows.net";

        public Task<CloudStorageAccount> CreateOrGetStorageAccountAsync(AzureClient azureClient)
        {
            throw new NotImplementedException();
        }

        public CloudStorageAccount CreateOrGetStorageAccount(AzureClient azureClient)
        {
            string rgName = azureClient.ResourceGroup;
            var storage = azureClient.azure.StorageAccounts.Define(azureClient.StorageAccountName)
                .WithRegion(azureClient.Region)
                .WithExistingResourceGroup(rgName)
                .Create();

            var storageKeys = storage.GetKeys();
            return CloudStorageAccount.Parse(string.Format(connectionStringFormat, azureClient.StorageAccountName, storageKeys[0].Value));
        }

        public async Task<CloudTable> CreateOrGetTableAsync(CloudStorageAccount storageAccount, string tableName)
        {
            var tableClient = storageAccount.CreateCloudTableClient(); ;

            // Retrieve a reference to the table.
            CloudTable table = tableClient.GetTableReference(tableName);

            // Create the table if it doesn't exist.
            await table.CreateIfNotExistsAsync();
            return table;
        }

        public CloudTable CreateOrGetTable(CloudStorageAccount storageAccount, string tableName)
        {
            var tableClient = storageAccount.CreateCloudTableClient(); ;

            // Retrieve a reference to the table.
            CloudTable table = tableClient.GetTableReference(tableName);

            // Create the table if it doesn't exist.
            Extensions.Synchronize(() => table.CreateIfNotExistsAsync());

            return table;
        }

        public async Task InsertOrMergeAsync<T>(T entity, CloudStorageAccount storageAccount, string tableName) where T : ITableEntity
        {
            var table = await CreateOrGetTableAsync(storageAccount, tableName);
            if (table == null)
            {
                return;
            }

            TableOperation tableOperation = TableOperation.InsertOrMerge(entity);
            await table.ExecuteAsync(tableOperation);
        }

        public void InsertOrMerge<T>(T entity, CloudStorageAccount storageAccount, string tableName) where T : ITableEntity
        {
            var table = CreateOrGetTable(storageAccount, tableName);
            if (table == null)
            {
                return;
            }

            TableOperation tableOperation = TableOperation.InsertOrMerge(entity);
            Extensions.Synchronize(() => table.ExecuteAsync(tableOperation));
        }

        public async Task<IEnumerable<T>> GetEntitiesAsync<T>(TableQuery<T> query, CloudStorageAccount storageAccount, string tableName) where T : ITableEntity, new()
        {
            if (query == null)
            {
                return null;
            }

            var table = CreateOrGetTable(storageAccount, tableName);
            if (table == null)
            {
                return null;
            }

            TableContinuationToken continuationToken = null;
            IEnumerable<T> results = null;
            do
            {
                var result = await table.ExecuteQuerySegmentedAsync<T>(query, continuationToken);
                if (result != null)
                {
                    results = results.Concat(result.Results);
                }

                continuationToken = result.ContinuationToken;
            } while (continuationToken != null);

            return results;
        }

        public IEnumerable<T> GetEntities<T>(TableQuery<T> query, CloudStorageAccount storageAccount, string tableName) where T : ITableEntity, new()
        {
            if (query == null)
            {
                return null;
            }

            var table = CreateOrGetTable(storageAccount, tableName);
            if (table == null)
            {
                return null;
            }

            TableContinuationToken continuationToken = null;
            IEnumerable<T> results = null;
            do
            {
                var result = Extensions.Synchronize(() => table.ExecuteQuerySegmentedAsync<T>(query, continuationToken));
                if (result != null)
                {
                    results = results.Concat(result.Results);
                }

                continuationToken = result.ContinuationToken;
            } while (continuationToken != null);

            return results;
        }
    }
}
