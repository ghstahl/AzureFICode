using AzureChaos.Constants;
using AzureChaos.Entity;
using AzureChaos.Enums;
using AzureChaos.Models;
using AzureChaos.Providers;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AzureChaos.Helper
{
    public class RuleEngineHelper
    {
        public static ScheduledRulesEntity ConvertToScheduledRuleEntity<T>(T entity, string sessionId, ActionType action, DateTime executionTime, VirtualMachineGroup virtualMachineGroup) where T : CrawlerResponseEntity
        {
            if (entity == null || !Mappings.FunctionNameMap.ContainsKey(virtualMachineGroup.ToString()))
            {
                return null;
            }

            return new ScheduledRulesEntity(virtualMachineGroup.ToString(), entity.RowKey)
            {
                executorEndPoint = Mappings.FunctionNameMap[virtualMachineGroup.ToString()],
                scheduledExecutionTime = executionTime,
                triggerData = GetTriggerData(entity, action),
                schedulerSessionId = sessionId
            };
        }

        public static ScheduledRulesEntity ConvertToScheduledRuleEntityForAvailabilitySet<T>(T entity, string sessionId, ActionType action, DateTime executionTime, bool domainFlage) where T : VirtualMachineCrawlerResponseEntity
        {
            if (entity == null || !Mappings.FunctionNameMap.ContainsKey(VirtualMachineGroup.AvailabilitySets.ToString()))
            {
                return null;
            }
            var combinationKey = string.Empty;
            if (domainFlage)
            {
                combinationKey = entity.AvailableSetId + "!" + entity.FaultDomain.ToString();
            }
            else
            {
                combinationKey = entity.AvailableSetId + "@" + entity.UpdateDomain.ToString();
            }
            return new ScheduledRulesEntity(VirtualMachineGroup.AvailabilitySets.ToString(), entity.RowKey)
            {
                executorEndPoint = Mappings.FunctionNameMap[VirtualMachineGroup.AvailabilitySets.ToString()],
                scheduledExecutionTime = executionTime,
                triggerData = GetTriggerData(entity, action),
                schedulerSessionId = sessionId,
                combinationKey = combinationKey
            };
        }

        public static ScheduledRulesEntity ConvertToScheduledRuleEntityForAvailabilityZone<T>(T entity, string sessionId, ActionType action, DateTime executionTime) where T : VirtualMachineCrawlerResponseEntity
        {
            if (!Mappings.FunctionNameMap.ContainsKey(VirtualMachineGroup.AvailabilityZones.ToString()))
            {
                return null;
            }

            return new ScheduledRulesEntity(VirtualMachineGroup.AvailabilityZones.ToString(), entity.RowKey)
            {
                executorEndPoint = Mappings.FunctionNameMap[VirtualMachineGroup.AvailabilityZones.ToString()],
                scheduledExecutionTime = executionTime,
                triggerData = GetTriggerData(entity, action),
                schedulerSessionId = sessionId,
                combinationKey = entity.RegionName + "!" + entity.AvailabilityZone
            };
        }

        public static string GetTriggerData(CrawlerResponseEntity crawlerResponse, ActionType action)
        {
            InputObject triggerdata = new InputObject();
            triggerdata.Action = action;
            triggerdata.ResourceName = crawlerResponse.ResourceName;
            triggerdata.ResourceGroup = crawlerResponse.ResourceGroupName;
            triggerdata.ScalesetId = crawlerResponse.PartitionKey.Replace("!", "/");
            return JsonConvert.SerializeObject(triggerdata);
        }

        public static List<VirtualMachineGroup> GetEnabledChaosSet(IStorageAccountProvider storageAccountProvider, AzureClient azureClient)
        {
            var storageAccount = storageAccountProvider.CreateOrGetStorageAccount(azureClient);
            var selectionQuery = TableQuery.GenerateFilterConditionForDate("scheduledExecutionTime", QueryComparisons.GreaterThanOrEqual,
                DateTimeOffset.UtcNow.AddMinutes(-azureClient.azureSettings.Chaos.SchedulerFrequency));
            TableQuery<ScheduledRulesEntity> scheduledQuery = new TableQuery<ScheduledRulesEntity>().Where(selectionQuery);
            var enabledChaos = Mappings.GetEnabledChaos(azureClient.azureSettings);
            var executedResults = storageAccountProvider.GetEntities<ScheduledRulesEntity>(scheduledQuery, storageAccount, azureClient.azureSettings.ScheduledRulesTable);
            if (executedResults == null || !executedResults.Any())
            {
                var chaos = enabledChaos.Where(x => x.Value == true);
                return chaos?.Select(x => x.Key).ToList();
            }

            var executedChaos = executedResults.Select(x => x.PartitionKey).Distinct().ToList();
            var excludedChaos = enabledChaos.Where(x => x.Value == true && !executedChaos.Contains(x.Key.ToString(), StringComparer.OrdinalIgnoreCase));
            return excludedChaos?.Select(x => x.Key).ToList();
        }
    }


    public class RandomResources
    {
        public RandomResources(AzureSettings azureSettings)
        {

        }
        public bool isVirtualMachineScaleSet { get; set; } = false;
        public bool isStandaloneVirtualMachine { get; set; } = false;
        public bool isAvailabilitySet { get; set; } = false;
        public bool isAvailabilityZone { get; set; } = false;

    }
}
