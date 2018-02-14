using AzureChaos.Core.Models;
using AzureChaos.Core.Models.Configs;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AzureChaos.Core.Providers
{
    public interface IStorageAccountProvider
    {
        Task<CloudStorageAccount> CreateOrGetStorageAccountAsync(AzureSettings azureSettings);

        CloudStorageAccount CreateOrGetStorageAccount(AzureClient azureClient);

        Task<CloudTable> CreateOrGetTableAsync(CloudStorageAccount storageAccount, string tableName);

        CloudTable CreateOrGetTable(CloudStorageAccount storageAccount, string tableName);

        Task InsertOrMergeAsync<T>(T entity, CloudStorageAccount storageAccount, string tableName) where T : ITableEntity;

        void InsertOrMerge<T>(T entity, CloudStorageAccount storageAccount, string tableName) where T : ITableEntity;

        Task<IEnumerable<T>> GetEntitiesAsync<T>(TableQuery<T> query, CloudStorageAccount storageAccount, string tableName) where T : ITableEntity, new();

        IEnumerable<T> GetEntities<T>(TableQuery<T> query, CloudStorageAccount storageAccount, string tableName) where T : ITableEntity, new();
    }
}