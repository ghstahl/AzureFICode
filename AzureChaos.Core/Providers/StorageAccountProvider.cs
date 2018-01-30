using AzureChaos.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureChaos.Providers
{
    /// <summary>The storage account provider</summary>
    public class StorageAccountProvider
    {
        private string storageAccountName = "st5ce22456";
        private string storageAccountKey = "KoCL+uvZZCpsuROXas0cGdm8jJCYurb5OiaNNhlNTzzl0oBhjoni72gryV70YwaO/sYXexhzG0ZcexQWhPTmdA==";
        public CloudStorageAccount storageAccount;
        public CloudTableClient tableClient;
        private const string StorageConStringFormat = "DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1};EndpointSuffix=core.windows.net";

        public StorageAccountProvider(ADConfiguration config)
        {
            this.storageAccount = (string.IsNullOrWhiteSpace(storageAccountName) || string.IsNullOrWhiteSpace(storageAccountKey)) ? CreateStorageAccount(config) : new CloudStorageAccount(new StorageCredentials(storageAccountName, storageAccountKey), true);
            this.tableClient = this.storageAccount.CreateCloudTableClient();
        }

        /// <summary>Create Storage Account.</summary>
        /// <param name="config">The Azure config.</param>
        private CloudStorageAccount CreateStorageAccount(ADConfiguration config)
        {
            var azure = AzureClient.GetAzure(config);
            string rgName = config.ResourceGroup;
            string storageAccountName = SdkContext.RandomResourceName("st", 10);
            var storage = azure.StorageAccounts.Define(storageAccountName)
                .WithRegion(config.Region)
                .WithExistingResourceGroup(rgName)
                .Create();

            var storageKeys = storage.GetKeys();
            this.storageAccountKey = storageKeys[0].Value;
            this.storageAccountName = storageAccountName;

            string storageConnectionString = "DefaultEndpointsProtocol=https;"
                + "AccountName=" + this.storageAccountName
                + ";AccountKey=" + this.storageAccountKey
                + ";EndpointSuffix=core.windows.net";

            return CloudStorageAccount.Parse(storageConnectionString);
        }

        /// <summary>Create new table for given storage account.</summary>
        /// <param name="tableName">The table name.</param>
        public CloudTable CreateOrGetTable(string tableName)
        {
            try
            {
                // Retrieve a reference to the table.
                CloudTable table = this.tableClient.GetTableReference(tableName);

                // Create the table if it doesn't exist.
                table.CreateIfNotExistsAsync();
                return table;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>Get the single entity</summary>
        /// <typeparam name="T">Type of the entity</typeparam>
        /// <param name="partitionKey">The partition key</param>
        /// <param name="rowKey">The rowkey</param>
        /// <param name="tableName">The table name</param>
        /// <returns>Returns the single entity of the type T.</returns>
        public async Task<T> GetSingleEntity<T>(string partitionKey, string rowKey, string tableName) where T : ITableEntity
        {
            try
            {
                var table = CreateOrGetTable(tableName);
                if (table == null)
                {
                    return default(T);
                }

                // Create a retrieve operation for the entity
                TableOperation retrieveOperation = TableOperation.Retrieve<T>(partitionKey, rowKey);

                // Execute the operation.
                TableResult retrievedResult = await table.ExecuteAsync(retrieveOperation);

                // Return the result.
                return (T)retrievedResult.Result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>Retrieve data from the storage table.</summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="tableQuery">The table query.</param>
        /// <param name="tableName">The table name.</param>
        /// <returns>Returns the Enumerable typed data.</returns>
        public async Task<IEnumerable<T>> GetEntities<T>(TableQuery<T> tableQuery, string tableName) where T : ITableEntity, new()
        {
            if (tableClient == null)
            {
                return null;
            }

            CloudTable table = tableClient.GetTableReference(tableName);
            if (table == null)
            {
                return null;
            }

            try
            {
                TableContinuationToken continuationToken = null;
                IEnumerable<T> results = null;
                do
                {
                    var result = await table.ExecuteQuerySegmentedAsync<T>(tableQuery, continuationToken);
                    if (result != null)
                    {
                        results = results.Concat(result.Results);
                    }

                    continuationToken = result.ContinuationToken;
                } while (continuationToken != null);

                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>Insert the single entity to table.</summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <param name="tableName">The table name.</param>
        public async void InsertEntity<T>(T entity, string tableName) where T : ITableEntity
        {
            try
            {
                var table = CreateOrGetTable(tableName);
                if (table == null)
                {
                    return;
                }

                TableOperation tableOperation = TableOperation.InsertOrReplace(entity);
                await table.ExecuteAsync(tableOperation);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>Insert or merge the entity into the table</summary>
        /// <typeparam name="T">Type of the Entity</typeparam>
        /// <param name="entity">The Entity</param>
        /// <param name="tableName">The Table name.</param>
        /// <returns></returns>
        public async Task InsertOrMerge<T>(T entity, string tableName) where T : ITableEntity
        {
            try
            {
                var table = CreateOrGetTable(tableName);
                if (table == null)
                {
                    return;
                }

                TableOperation tableOperation = TableOperation.InsertOrMerge(entity);
                await table.ExecuteAsync(tableOperation);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
