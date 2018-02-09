using AzureChaos.Entity;
using AzureChaos.Enums;
using Microsoft.Azure.Management.Compute.Fluent;
using System;

namespace AzureChaos.Helper
{
    public static class VirtualMachineHelper
    {
        /// <summary>Convert the Virtual machine to virtual machine crawler response entity.</summary>
        /// <param name="virtualMachine">The virtual machine.</param>
        /// <param name="vmGroup">Vm group name.</param>
        /// <returns></returns>
        public static VirtualMachineCrawlerResponseEntity ConvertToVirtualMachineEntity(IVirtualMachine virtualMachine, string vmGroup = "")
        {
            VirtualMachineCrawlerResponseEntity virtualMachineCrawlerResponseEntity = new VirtualMachineCrawlerResponseEntity("crawlvms", Guid.NewGuid().ToString());
            virtualMachineCrawlerResponseEntity.EntryInsertionTime = DateTime.Now;
            //resourceGroupCrawlerResponseEntity.EventType = data?.Action;
            virtualMachineCrawlerResponseEntity.RegionName = virtualMachine.RegionName;
            virtualMachineCrawlerResponseEntity.ResourceGroupName = virtualMachine.ResourceGroupName;
            virtualMachineCrawlerResponseEntity.ResourceName = virtualMachine.Name;
            virtualMachineCrawlerResponseEntity.AvailableSetId = virtualMachine.AvailabilitySetId;
            virtualMachineCrawlerResponseEntity.UpdateDomain = virtualMachine.InstanceView.PlatformUpdateDomain;
            virtualMachineCrawlerResponseEntity.FaultDomain = virtualMachine.InstanceView.PlatformFaultDomain;
            virtualMachineCrawlerResponseEntity.ResourceType = virtualMachine.Type;
            virtualMachineCrawlerResponseEntity.Id = virtualMachine.Id;
            virtualMachineCrawlerResponseEntity.VirtualMachineGroup = string.IsNullOrWhiteSpace(vmGroup) ? VirtualMachineGroup.VirtualMachines.ToString() : vmGroup;
            return virtualMachineCrawlerResponseEntity;
        }

        /// <summary>Convert the Virtual machine to virtual machine crawler response entity.</summary>
        /// <param name="scaleSetVM">The virtual machine.</param>
        /// <param name="resourceGroup">The resource group name.</param>
        /// <param name="scaleSetName">Scale set name of the vm.</param>
        /// <param name="vmGroup">Virtual machine group name.</param>
        /// <returns></returns>
        public static VirtualMachineCrawlerResponseEntity ConvertToVirtualMachineEntity(IVirtualMachineScaleSetVM scaleSetVM, string resourceGroup, string scaleSetId, string vmGroup = "")
        {
            VirtualMachineCrawlerResponseEntity virtualMachineCrawlerResponseEntity = new VirtualMachineCrawlerResponseEntity("crawlvms", Guid.NewGuid().ToString());
            virtualMachineCrawlerResponseEntity.EntryInsertionTime = DateTime.Now;
            //resourceGroupCrawlerResponseEntity.EventType = data?.Action;
            virtualMachineCrawlerResponseEntity.RegionName = scaleSetVM.RegionName;
            virtualMachineCrawlerResponseEntity.ResourceGroupName = resourceGroup;
            virtualMachineCrawlerResponseEntity.ResourceName = scaleSetVM.Name;
            virtualMachineCrawlerResponseEntity.AvailableSetId = scaleSetVM.AvailabilitySetId;
            virtualMachineCrawlerResponseEntity.UpdateDomain = scaleSetVM.InstanceView.PlatformUpdateDomain;
            virtualMachineCrawlerResponseEntity.FaultDomain = scaleSetVM.InstanceView.PlatformFaultDomain;
            virtualMachineCrawlerResponseEntity.ResourceType = scaleSetVM.Type;
            virtualMachineCrawlerResponseEntity.ScaleSetId = scaleSetId;
            virtualMachineCrawlerResponseEntity.Id = scaleSetVM.Id;
            virtualMachineCrawlerResponseEntity.VirtualMachineGroup = string.IsNullOrWhiteSpace(vmGroup) ? VirtualMachineGroup.VirtualMachines.ToString() : vmGroup;
            return virtualMachineCrawlerResponseEntity;
        }
    }
}