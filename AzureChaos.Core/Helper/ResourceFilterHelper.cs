using AzureChaos.Models;
using AzureChaos.Providers;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureChaos.Helper
{
    public class ResourceFilterHelper
    {
        private static Random random = new Random();

        // TODO - this is not thread safe will modify the code.
        // just shuffle method to shuffle the list  of items to get the random  items
        public static void Shuffle<T>(IList<T> list)
        {
            if (list == null || !list.Any())
            {
                return;
            }

            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static List<T> QueryByMeanTime<T>(CloudStorageAccount storageAccount, IStorageAccountProvider storageAccountProvider,
            AzureSettings azureSettings, string tableName, string filter = "") where T : ITableEntity, new()
        {
            TableQuery<T> tableQuery = new TableQuery<T>();
            tableQuery = tableQuery.Where(GetInsertionDatetimeFilter(azureSettings, filter));
            var resultsSet = storageAccountProvider.GetEntities<T>(tableQuery, storageAccount, tableName);
            return resultsSet.ToList();
        }

        public static async Task<List<T>> QueryByMeanTimeAsync<T>(CloudStorageAccount storageAccount, IStorageAccountProvider storageAccountProvider,
            AzureSettings azureSettings, string tableName, string filter = "") where T : ITableEntity, new()
        {
            TableQuery<T> tableQuery = new TableQuery<T>();
            tableQuery = tableQuery.Where(GetInsertionDatetimeFilter(azureSettings, filter));
            var resultsSet = await storageAccountProvider.GetEntitiesAsync<T>(tableQuery, storageAccount, tableName);
            return resultsSet.ToList();
        }

        public static List<T> QueryByPartitionKey<T>(CloudStorageAccount storageAccount, IStorageAccountProvider storageAccountProvider,
          string partitionKey, string tableName) where T : ITableEntity, new()
        {
            TableQuery<T> tableQuery = new TableQuery<T>();
            tableQuery = tableQuery.Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey));
            var resultsSet = storageAccountProvider.GetEntities<T>(tableQuery, storageAccount, tableName);
            return resultsSet.ToList();
        }

        public static async Task<List<T>> QueryByPartitionKeyAsync<T>(CloudStorageAccount storageAccount, IStorageAccountProvider storageAccountProvider,
         string partitionKey, string tableName) where T : ITableEntity, new()
        {
            TableQuery<T> tableQuery = new TableQuery<T>();
            tableQuery = tableQuery.Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey));
            var resultsSet = await storageAccountProvider.GetEntitiesAsync<T>(tableQuery, storageAccount, tableName);
            return resultsSet.ToList();
        }

        public static List<T> QueryByPartitionKeyAndRowKey<T>(CloudStorageAccount storageAccount, IStorageAccountProvider storageAccountProvider,
            string partitionKey, string rowKey, string tableName) where T : ITableEntity, new()
        {
            TableQuery<T> tableQuery = new TableQuery<T>();
            var dateFilter = TableQuery.CombineFilters(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, rowKey));
            tableQuery = tableQuery.Where(dateFilter);
            var resultsSet = storageAccountProvider.GetEntities<T>(tableQuery, storageAccount, tableName);
            return resultsSet.ToList();
        }

        public static async Task<List<T>> QueryByPartitionKeyAndRowKeyAsync<T>(CloudStorageAccount storageAccount, IStorageAccountProvider storageAccountProvider,
            string partitionKey, string rowKey, string tableName) where T : ITableEntity, new()
        {
            TableQuery<T> tableQuery = new TableQuery<T>();
            var dateFilter = TableQuery.CombineFilters(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, rowKey));
            tableQuery = tableQuery.Where(dateFilter);
            var resultsSet = await storageAccountProvider.GetEntitiesAsync<T>(tableQuery, storageAccount, tableName);
            return resultsSet.ToList();
        }

        private static string GetInsertionDatetimeFilter(AzureSettings azureSettings, string combinedFilter = "")
        {
            var dateFilter = TableQuery.CombineFilters(TableQuery.GenerateFilterConditionForDate("EntryInsertionTime", QueryComparisons.LessThanOrEqual, DateTimeOffset.UtcNow),
                TableOperators.And,
                TableQuery.GenerateFilterConditionForDate("EntryInsertionTime", QueryComparisons.GreaterThanOrEqual, DateTimeOffset.UtcNow.AddHours(-azureSettings.Chaos.SchedulerFrequency)));
            if (string.IsNullOrWhiteSpace(combinedFilter))
            {
                return dateFilter;
            }

            return TableQuery.CombineFilters(dateFilter, TableOperators.And, combinedFilter);
        }
    }
}
