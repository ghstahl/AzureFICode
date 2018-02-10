using AzureChaos.Constants;
using AzureChaos.Entity;
using AzureChaos.Enums;
using AzureChaos.Models;
using AzureChaos.Providers;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

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
        public static ScheduledRulesEntity ConvertToScheduledRuleEntityForAvailabilitySet<T>(T entity, string sessionId, ActionType action, DateTime executionTime, bool domainFlage) where T : VirtualMachineCrawlerResponseEntity
        {
            if (!Endpoints.ComputeExecuterEndpoints.ContainsKey(entity.ResourceType))
            {
                return null;
            }
            var partitonKeyValue = string.Empty;
            if (domainFlage)
            {
                partitonKeyValue = entity.AvailableSetId + "@" + entity.FaultDomain.ToString();
            }
            else
            {
                partitonKeyValue = entity.AvailableSetId + "@" + entity.UpdateDomain.ToString();
            }
            return new ScheduledRulesEntity(partitonKeyValue, entity.RowKey)
            {
                executorEndPoint = Endpoints.ComputeExecuterEndpoints[entity.ResourceType],
                scheduledExecutionTime = executionTime,
                triggerData = GetTriggerData(entity, action.ToString()),
                schedulerSessionId = sessionId
            };
        }
        public static ScheduledRulesEntity ConvertToScheduledRuleEntityForAvailabilityZone<T>(T entity, string sessionId, ActionType action, DateTime executionTime) where T : VirtualMachineCrawlerResponseEntity
        {
            if (!Endpoints.ComputeExecuterEndpoints.ContainsKey(entity.ResourceType))
            {
                return null;
            }

            return new ScheduledRulesEntity(entity.RegionName + "!" + entity.AvailabilityZone, entity.RowKey)
            {
                executorEndPoint = Endpoints.ComputeExecuterEndpoints[entity.ResourceType],
                scheduledExecutionTime = executionTime,
                triggerData = GetTriggerData(entity, action.ToString()),
                schedulerSessionId = sessionId
            };
        }

        public static ScheduledRulesEntity ConvertToScheduledRuleEntityForAvailabilitySet<T>(T entity, string sessionId, ActionType action, DateTime executionTime, bool domainFlage) where T : VirtualMachineCrawlerResponseEntity
        {
            if (entity == null || !Endpoints.FunctionNameMap.ContainsKey(entity.ResourceType))
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
                executorEndPoint = Endpoints.FunctionNameMap[entity.ResourceType],
                scheduledExecutionTime = executionTime,
                triggerData = GetTriggerData(entity, action),
                schedulerSessionId = sessionId,
                combinationKey = combinationKey
            };
        }

        public static ScheduledRulesEntity ConvertToScheduledRuleEntityForAvailabilityZone<T>(T entity, string sessionId, ActionType action, DateTime executionTime) where T : VirtualMachineCrawlerResponseEntity
        {
            if (!Endpoints.FunctionNameMap.ContainsKey(entity.ResourceType))
            {
                return null;
            }

            return new ScheduledRulesEntity(VirtualMachineGroup.AvailabilityZones.ToString(), entity.RowKey)
            {
                executorEndPoint = Endpoints.FunctionNameMap[entity.ResourceType],
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

        public static RandomResources PickRandomResource(IStorageAccountProvider storageAccountProvider, AzureClient azureClient)
        {
            RandomResources randomResource = new RandomResources();
            List<int> myRandomValues = new List<int>(new int[] { 1, 2, 3, 4 });
            var storageAccount = storageAccountProvider.CreateOrGetStorageAccount(azureClient);
            var selectionQuery = TableQuery.GenerateFilterConditionForDate("scheduledExecutionTime", QueryComparisons.GreaterThanOrEqual, DateTimeOffset.UtcNow.AddMinutes(-120));
            TableQuery<ScheduledRulesEntity> scheduledQuery = new TableQuery<ScheduledRulesEntity>().Where(selectionQuery);
            var executedResults = storageAccountProvider.GetEntities<ScheduledRulesEntity>(scheduledQuery, storageAccount, azureClient.azureSettings.ScheduledRulesTable);
            if (executedResults != null)
            {
                AssignResource(myRandomValues);
            }
            else
            {
                foreach (var eachExecutedResults in executedResults)
                {
                    if(eachExecutedResults.PartitionKey == VirtualMachineGroup.VirtualMachines.ToString())
                    {
                        myRandomValues.Remove(1);
                    }
                    else if (eachExecutedResults.PartitionKey == VirtualMachineGroup.ScaleSets.ToString())
                    {
                        myRandomValues.Remove(2);
                    }
                    else if (eachExecutedResults.PartitionKey == VirtualMachineGroup.AvailabilitySets.ToString())
                    {
                        myRandomValues.Remove(3);
                    }
                    else if (eachExecutedResults.PartitionKey == VirtualMachineGroup.AvailabilitySets.ToString())
                    {
                        myRandomValues.Remove(4);
                    }
                    break;
                }
            }

            return AssignResource(myRandomValues);

        }

        public static RandomResources AssignResource(List<int> myRandomValues)
        {
            RandomResources randomResource = new RandomResources();
            Random random = new Random();
            var randomSelectedValue = myRandomValues[random.Next(0, myRandomValues.Count - 1)];
            if (randomSelectedValue ==1 )
            {
                randomResource.isStandaloneVirtualMachine = true;
            }
            else if (randomSelectedValue == 2)
            {
                randomResource.isVirtualMachineScaleSet = true;
            }
            else if (randomSelectedValue == 3)
            {
                randomResource.isAvailabilitySet = true;
            }
            else 
            {
                randomResource.isAvailabilityZone = true;
            }
            return randomResource;
        }
    }

    public class RandomResources
    {
        public bool isVirtualMachineScaleSet { get; set; } = false;
        public bool isStandaloneVirtualMachine { get; set; } = false;
        public bool isAvailabilitySet { get; set; } = false;
        public bool isAvailabilityZone { get; set; } = false;

    }
}
