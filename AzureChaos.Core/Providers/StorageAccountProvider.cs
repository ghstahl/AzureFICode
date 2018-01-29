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
        private string storageAccountName = "";
        private string storageAccountKey = "";
        private CloudStorageAccount storageAccount;
        private CloudTableClient tableClient;
        private const string StorageConStringFormat = "DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1};EndpointSuffix=core.windows.net";

        public StorageAccountProvider() { }
        public StorageAccountProvider(string storageAccountName, string storageAccountKey)
        {
            this.storageAccountName = storageAccountName;
            this.storageAccountKey = storageAccountKey;
            this.storageAccount = new CloudStorageAccount(new StorageCredentials(storageAccountName, storageAccountKey), true);
            this.tableClient = this.storageAccount.CreateCloudTableClient();
        }

        public CloudStorageAccount CreateStorageAccountIfNotExist(ADConfiguration config)
        {
            if (string.IsNullOrWhiteSpace(this.storageAccountName) || string.IsNullOrWhiteSpace(this.storageAccountKey))
            {
                storageAccount = CreateStorageAccount(config);
            }
            else
            {
                string storageConnectionString = string.Format(StorageConStringFormat, this.storageAccountName, this.storageAccountKey);
                var account = CloudStorageAccount.Parse(storageConnectionString);
                if (account == null)
                {
                    storageAccount = CreateStorageAccount(config);
                }
                else
                {
                    this.storageAccount = account;
                }
            }

            return storageAccount;
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
                .WithNewResourceGroup(rgName)
                .Create();

            var storageKeys = storage.GetKeys();
            this.storageAccountKey = storageKeys[0].Value;
            this.storageAccountName = storageAccountName;

            string storageConnectionString = "DefaultEndpointsProtocol=https;"
                + "AccountName=" + this.storageAccountName
                + ";AccountKey=" + this.storageAccountKey
                + ";EndpointSuffix=core.windows.net";

            var account = CloudStorageAccount.Parse(storageConnectionString);
            return account;

        }

        /// <summary>Retrieve data from the storage table.</summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="tableQuery">The table query.</param>
        /// <param name="tableName">The table name.</param>
        /// <returns>Returns the Enumerable typed data.</returns>
        public async Task<IEnumerable<T>> RetrieveTableData<T>(TableQuery<T> tableQuery, string tableName) where T : ITableEntity, new()
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

        /// <summary>Create new table for given storage account.</summary>
        /// <param name="tableName">The table name.</param>
        public CloudTable CreateTable(string tableName)
        {
            if (tableClient == null)
            {
                return null;
            }

            try
            {
                // Retrieve a reference to the table.
                CloudTable table = tableClient.GetTableReference(tableName);

                // Create the table if it doesn't exist.
                table.CreateIfNotExistsAsync();
                return table;
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
                var table = CreateTable(tableName);
                if (table == null)
                {
                    return;
                }

                TableOperation tableOperation = TableOperation.Insert(entity);
                await table.ExecuteAsync(tableOperation);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>Insert the multiple entities as a batch.</summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="entities">List of entities.</param>
        /// <param name="tableName">Table name.</param>
        public async void InsertEntities<T>(List<T> entities, string tableName) where T : ITableEntity
        {
            try
            {
                var table = CreateTable(tableName);
                if (table == null || entities == null || !entities.Any())
                {
                    return;
                }

                TableBatchOperation batchOperation = new TableBatchOperation();
                foreach (var entity in entities)
                {
                    batchOperation.Insert(entity);
                }

                // insert the multiple entities as batch
                await table.ExecuteBatchAsync(batchOperation);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
