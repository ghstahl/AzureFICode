using AzureChaos.Entity;
using AzureChaos.Enums;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AzureChaos.Helper
{
    public static class VirtualMachineHelper
    {
        /// <summary>Convert the Virtual machine to virtual machine crawler response entity.</summary>
        /// <param name="virtualMachine">The virtual machine.</param>
        /// <param name="vmGroup">Vm group name.</param>
        /// <returns></returns>
        public static VirtualMachineCrawlerResponseEntity ConvertToVirtualMachineEntity(IVirtualMachine virtualMachine, string partitionKey, string vmGroup = "")
        {
            VirtualMachineCrawlerResponseEntity virtualMachineCrawlerResponseEntity = new VirtualMachineCrawlerResponseEntity(partitionKey, virtualMachine.Id.Replace("/", "!"))
            {
                EntryInsertionTime = DateTime.Now,
                RegionName = virtualMachine.RegionName,
                ResourceGroupName = virtualMachine.ResourceGroupName,
                ResourceName = virtualMachine.Name,
                AvailableSetId = virtualMachine.AvailabilitySetId,
                UpdateDomain = virtualMachine.InstanceView.PlatformUpdateDomain,
                FaultDomain = virtualMachine.InstanceView.PlatformFaultDomain,
                ResourceType = virtualMachine.Type,
                Id = virtualMachine.Id,
                AvailabilityZone = virtualMachine.AvailabilityZones.Count > 0 ?
                    int.Parse(virtualMachine.AvailabilityZones.FirstOrDefault().Value) : 0,
                VirtualMachineGroup = string.IsNullOrWhiteSpace(vmGroup) ? VirtualMachineGroup.VirtualMachines.ToString() : vmGroup,
                State = virtualMachine.PowerState.Value
            };

            return virtualMachineCrawlerResponseEntity;
        }

        /// <summary>Convert the Virtual machine to virtual machine crawler response entity.</summary>
        /// <param name="scaleSetVM">The virtual machine.</param>
        /// <param name="resourceGroup">The resource group name.</param>
        /// <param name="scaleSetName">Scale set name of the vm.</param>
        /// <param name="vmGroup">Virtual machine group name.</param>
        /// <returns></returns>
        public static VirtualMachineCrawlerResponseEntity ConvertToVirtualMachineEntity(IVirtualMachineScaleSetVM scaleSetVM, string resourceGroup, string scaleSetId, string partitionKey, int? availabilityZone, string vmGroup = "")
        {
            VirtualMachineCrawlerResponseEntity virtualMachineCrawlerResponseEntity = new VirtualMachineCrawlerResponseEntity(partitionKey, scaleSetVM.Id.Replace("/", "!"))
            {
                EntryInsertionTime = DateTime.Now,
                RegionName = scaleSetVM.RegionName,
                ResourceGroupName = resourceGroup,
                ResourceName = scaleSetVM.Name,
                AvailableSetId = scaleSetVM.AvailabilitySetId,
                UpdateDomain = scaleSetVM.InstanceView.PlatformUpdateDomain,
                FaultDomain = scaleSetVM.InstanceView.PlatformFaultDomain,
                ResourceType = scaleSetVM.Type,
                ScaleSetId = scaleSetId,
                Id = scaleSetVM.Id,
                AvailabilityZone = (availabilityZone != 0) ? availabilityZone : null,
                VirtualMachineGroup = string.IsNullOrWhiteSpace(vmGroup) ? VirtualMachineGroup.VirtualMachines.ToString() : vmGroup,
                State = scaleSetVM.PowerState.Value
            };

            return virtualMachineCrawlerResponseEntity;
        }

        /// <summary>Create the table batch operation for the scheduled entity for the set of virtual machines.</summary>
        /// <param name="filteredVmSet">Set of virtual machines.</param>
        /// <param name="schedulerFrequency">Schedule frequency, it will be reading from the config</param>
        /// <returns></returns>
        public static TableBatchOperation CreateScheduleEntity(IList<VirtualMachineCrawlerResponseEntity> filteredVmSet, int schedulerFrequency)
        {
            TableBatchOperation tableBatchOperation = new TableBatchOperation();
            Random random = new Random();
            DateTime randomExecutionDateTime = DateTime.UtcNow.AddMinutes(random.Next(5, schedulerFrequency - 10));
            var sessionId = Guid.NewGuid().ToString();
            foreach (var item in filteredVmSet)
            {
                var actionType = GetAction(item.State);
                if (actionType == ActionType.Unknown)
                {
                    continue;
                }

                tableBatchOperation.InsertOrMerge(RuleEngineHelper.ConvertToScheduledRuleEntity<VirtualMachineCrawlerResponseEntity>(item, sessionId, actionType, randomExecutionDateTime));
            }

            return tableBatchOperation;
        }

        /// <summary>Get the action based on the current state of the virtual machine.</summary>
        /// <param name="state">Current state of the virtual machine.</param>
        /// <returns></returns>
        public static ActionType GetAction(string state)
        {
            PowerState powerState = PowerState.Parse(state);
            if (powerState == PowerState.Running || powerState == PowerState.Starting)
            {
                return ActionType.PowerOff;
            }
            else if (powerState == PowerState.Stopping || powerState == PowerState.Stopped)
            {
                return ActionType.Start;
            }

            return ActionType.Unknown;
        }
    }
}
