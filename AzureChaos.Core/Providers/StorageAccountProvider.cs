using AzureChaos.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Threading.Tasks;

namespace AzureChaos.Providers
{
    /// <summary>The storage account provider</summary>
    public class StorageAccountProvider
    {
        public CloudStorageAccount storageAccount;
        public CloudTableClient tableClient;
        private AzureClient azureClient;
        private const string StorageConStringFormat = "DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1};EndpointSuffix=core.windows.net";

        public StorageAccountProvider(AzureClient azureClient)
        {
            this.azureClient = azureClient;
            this.storageAccount = CreateStorageAccount();
            this.tableClient = this.storageAccount.CreateCloudTableClient();
        }

        /// <summary>Create Storage Account.</summary>
        /// <param name="config">The Azure config.</param>
        private CloudStorageAccount CreateStorageAccount()
        {
            string rgName = this.azureClient.ResourceGroup;
            var storage = azureClient.azure.StorageAccounts.Define(azureClient.StorageAccountName)
                .WithRegion(this.azureClient.Region)
                .WithExistingResourceGroup(rgName)
                .Create();

            var storageKeys = storage.GetKeys();
            return CloudStorageAccount.Parse(string.Format(StorageConStringFormat, this.azureClient.StorageAccountName, storageKeys[0].Value));
        }

        /// <summary>Create new table for given storage account.</summary>
        /// <param name="tableName">The table name.</param>
        public async Task<CloudTable> CreateOrGetTable(string tableName)
        {
            try
            {
                if (this.tableClient == null)
                {
                    throw new ArgumentNullException("Table client is null");
                }

                // Retrieve a reference to the table.
                CloudTable table = this.tableClient.GetTableReference(tableName);

                // Create the table if it doesn't exist.
                await table.CreateIfNotExistsAsync();
                return table;
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
                var table = await CreateOrGetTable(tableName);
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
