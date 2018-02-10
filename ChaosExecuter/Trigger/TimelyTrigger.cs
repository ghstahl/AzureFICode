using AzureChaos.Constants;
using AzureChaos.Entity;
using AzureChaos.Helper;
using AzureChaos.Models;
using AzureChaos.Providers;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChaosExecuter.Trigger
{
    /// <summary>Scheduled trigger - will pick the lastest rules from the scheduled rules table and execute the executer if the execution time is near.</summary>
    public static class TimelyTrigger
    {
        private static readonly AzureClient AzureClient = new AzureClient();
        private static readonly IStorageAccountProvider StorageProvider = new StorageAccountProvider();

        /// <summary>Every 5 mints </summary>
        [FunctionName("TimelyTrigger")]
        public async static void Run([TimerTrigger("0 */2 * * * *")]TimerInfo myTimer, [OrchestrationClient]
        DurableOrchestrationClient starter, TraceWriter log)
        {
            log.Info($"Chaos trigger function execution started: {DateTime.Now}");
            var resultForExecution = GetScheduledRulesForExecution();
            if (resultForExecution == null)
            {
                log.Info($"Chaos trigger no entries to trigger");
                return;
            }

            foreach (var result in resultForExecution)
            {
                var partitionKey = result.PartitionKey.Replace("!", "/");
                if (!Endpoints.FunctionNameMap.ContainsKey(partitionKey))
                {
                    continue;
                }

                string functionName = Endpoints.FunctionNameMap[partitionKey];
                log.Info($"Chaos trigger: invoking function: " + functionName);
                await starter.StartNewAsync(functionName, result.triggerData);
            }

            log.Info($"Chaos trigger function execution ended: {DateTime.Now}");
        }

        /// <summary>Get the scheduled rules for the chaos execution.</summary>
        /// <returns></returns>
        private static IEnumerable<ScheduledRulesEntity> GetScheduledRulesForExecution()
        {
            var storageAccount = StorageProvider.CreateOrGetStorageAccount(AzureClient);
            var activityEntities = ResourceFilterHelper.QueryByMeanTime<EventActivityEntity>(storageAccount, StorageProvider, AzureClient.azureSettings, AzureClient.azureSettings.ActivityLogTable);
            var activityEntitiesResourceIds = (activityEntities == null || !activityEntities.Any()) ? new List<string>() : activityEntities.Select(x => x.Id);

            var dateFilterByUtc = TableQuery.GenerateFilterConditionForDate("scheduledExecutionTime", QueryComparisons.GreaterThanOrEqual,
                  DateTimeOffset.UtcNow);

            var dateFilterByFrequency = TableQuery.GenerateFilterConditionForDate("scheduledExecutionTime", QueryComparisons.LessThanOrEqual,
                  DateTimeOffset.UtcNow.AddMinutes(5));//TODO: Will be moving to config.

            var filter = TableQuery.CombineFilters(dateFilterByUtc, TableOperators.And, dateFilterByFrequency);
            TableQuery<ScheduledRulesEntity> scheduledQuery = new TableQuery<ScheduledRulesEntity>().Where(filter);

            var resultSet = StorageProvider.GetEntities<ScheduledRulesEntity>(scheduledQuery, storageAccount, AzureClient.azureSettings.ScheduledRulesTable);
            if (resultSet == null)
            {
                return null;
            }

            return resultSet.Where(x => !activityEntitiesResourceIds.Contains(x.RowKey.Replace("!", "/")));
        }
    }
}
