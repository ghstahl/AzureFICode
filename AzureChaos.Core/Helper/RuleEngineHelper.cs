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
            if (!Endpoints.ComputeExecuterEndpoints.ContainsKey(entity.ResourceType))
            {
                return null;
            }

            return new ScheduledRulesEntity(entity.ResourceType.Replace("/", "!"), entity.RowKey)
            {
                executorEndPoint = Endpoints.ComputeExecuterEndpoints[entity.ResourceType],
                scheduledExecutionTime = executionTime,
                triggerData = GetTriggerData(entity, action.ToString()),
                schedulerSessionId = sessionId
            };
        }

        public static string GetTriggerData(CrawlerResponseEntity crawlerResponse, string action)
        {
            TriggerData triggerdata = new TriggerData();
            triggerdata.Action = action;
            triggerdata.ResourceName = crawlerResponse.ResourceName;
            triggerdata.ResourceGroup = crawlerResponse.ResourceGroupName;
            return JsonConvert.SerializeObject(triggerdata);
        }
    }
}
