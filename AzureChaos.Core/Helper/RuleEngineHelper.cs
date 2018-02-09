using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using AzureChaos.Core.Entity;
using AzureChaos.Entity;
using Microsoft.Azure.Management.Compute.Fluent;
using Newtonsoft.Json;

namespace AzureChaos.Core.Helper
{
    public class RuleEngineHelper
    {
        public static ScheduledRulesEntity ConvertToScheduledRuleEntity(VirtualMachineCrawlerResponseEntity virtualMachine, string sessionId, int schedulerFrequency)
        {
            Random random = new Random();
            ScheduledRulesEntity scheduledRuleEntity = new ScheduledRulesEntity();
            DateTime randomExecutionDateTime = DateTime.UtcNow.AddMinutes(random.Next(5, schedulerFrequency - 10));
            scheduledRuleEntity.PartitionKey = "PowerOff";
            scheduledRuleEntity.RowKey = virtualMachine.Id.Replace("/", "-");
            scheduledRuleEntity.executorEndPoint =
                "https://microsoftchaosdemo.azurewebsites.net/api/vmexecuter?code=CiLRE7vjHKhxYea27KH6Ca0TEt7JftQTAm5e1f7VKG0whVPUuF23fw==";
            scheduledRuleEntity.isRollBack = false;
            scheduledRuleEntity.triggerData = GetTriggerData(virtualMachine, "PowerOff");
            scheduledRuleEntity.scheduledExecutionTime = randomExecutionDateTime;
            scheduledRuleEntity.schedulerSessionId = sessionId;
            return scheduledRuleEntity;
        }

        public static string GetTriggerData(VirtualMachineCrawlerResponseEntity virtualmachine, string action)
        {
            TriggerData triggerdata = new TriggerData();
            triggerdata.Action = action;
            triggerdata.ResourceName = virtualmachine.ResourceName;
            triggerdata.ResourceGroup = virtualmachine.ResourceGroupName;
            return JsonConvert.SerializeObject(triggerdata);
        }
    }

    public class TriggerData
    {
        public string Action { get; set; }
        public string ResourceName { get; set; }
        public string ResourceGroup { get; set; }
    }
}