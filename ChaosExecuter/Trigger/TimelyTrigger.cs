using AzureChaos.Constants;
using AzureChaos.Entity;
using AzureChaos.Models;
using AzureChaos.Providers;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;

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
            log.Info($"Chaos trigger function execution started: {DateTime.UtcNow}");
            var resultForExecution = GetScheduledRulesForExecution();
            if (resultForExecution == null)
            {
                log.Info($"Chaos trigger no entries to trigger");
                return;
            }

            foreach (var result in resultForExecution)
            {
                var partitionKey = result.PartitionKey.Replace("!", "/");
                if (!Mappings.FunctionNameMap.ContainsKey(partitionKey))
                {
                    continue;
                }

                string functionName = Mappings.FunctionNameMap[partitionKey];
                log.Info($"Chaos trigger: invoking function: " + functionName);
                await starter.StartNewAsync(functionName, result.triggerData);
            }

            log.Info($"Chaos trigger function execution ended: {DateTime.UtcNow}");
        }

        /// <summary>Get the scheduled rules for the chaos execution.</summary>
        /// <returns></returns>
        private static IEnumerable<ScheduledRulesEntity> GetScheduledRulesForExecution()
        {
            var storageAccount = StorageProvider.CreateOrGetStorageAccount(AzureClient);

            var dateFilterByUtc = TableQuery.GenerateFilterConditionForDate("scheduledExecutionTime", QueryComparisons.GreaterThanOrEqual,
                  DateTimeOffset.UtcNow);

            var dateFilterByFrequency = TableQuery.GenerateFilterConditionForDate("scheduledExecutionTime", QueryComparisons.LessThanOrEqual,
                  DateTimeOffset.UtcNow.AddMinutes(AzureClient.azureSettings.Chaos.TriggerFrequency));

            var filter = TableQuery.CombineFilters(dateFilterByUtc, TableOperators.And, dateFilterByFrequency);
            TableQuery<ScheduledRulesEntity> scheduledQuery = new TableQuery<ScheduledRulesEntity>().Where(filter);

           return StorageProvider.GetEntities<ScheduledRulesEntity>(scheduledQuery, storageAccount, AzureClient.azureSettings.ScheduledRulesTable);
        }
    }
}
