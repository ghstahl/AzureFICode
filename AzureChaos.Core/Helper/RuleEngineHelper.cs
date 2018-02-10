using AzureChaos.Constants;
using AzureChaos.Entity;
using AzureChaos.Enums;
using AzureChaos.Models;
using Newtonsoft.Json;
using System;

namespace AzureChaos.Helper
{
    public class RuleEngineHelper
    {
        public static ScheduledRulesEntity ConvertToScheduledRuleEntity<T>(T entity, string sessionId, ActionType action, DateTime executionTime) where T : CrawlerResponseEntity
        {
            if (entity == null || !Endpoints.FunctionNameMap.ContainsKey(entity.ResourceType))
            {
                return null;
            }

            return new ScheduledRulesEntity(entity.ResourceType.Replace("/", "!"), entity.RowKey)
            {
                executorEndPoint = Endpoints.FunctionNameMap[entity.ResourceType],
                scheduledExecutionTime = executionTime,
                triggerData = GetTriggerData(entity, action),
                schedulerSessionId = sessionId
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
    }
}
