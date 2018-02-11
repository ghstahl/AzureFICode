using AzureChaos.Enums;
using AzureChaos.Models;
using System.Collections.Generic;

namespace AzureChaos.Constants
{
    public class Mappings
    {
        public static IDictionary<VirtualMachineGroup, bool> GetEnabledChaos(AzureSettings azureSettings)
        {
            return new Dictionary<VirtualMachineGroup, bool>()
            {
                { VirtualMachineGroup.AvailabilitySets, azureSettings.Chaos.AvailabilitySetChaos.Enabled},
                { VirtualMachineGroup.VirtualMachines, azureSettings.Chaos.VirtualMachineChaos.Enabled},
                { VirtualMachineGroup.AvailabilityZones, azureSettings.Chaos.AvailabilityZoneChaos.Enabled},
                { VirtualMachineGroup.ScaleSets, azureSettings.Chaos.ScaleSetChaos.Enabled}
            };
        }

        public static Dictionary<string, string> FunctionNameMap = new Dictionary<string, string>()
        {
            { VirtualMachineGroup.VirtualMachines.ToString(), "vmexecuter" },
            { VirtualMachineGroup.ScaleSets.ToString(), "scalesetvmexecuter" },
            { VirtualMachineGroup.AvailabilitySets.ToString(), "vmexecuter" },
            { VirtualMachineGroup.AvailabilityZones.ToString(), "vmexecuter" },
        };



        ///Microsoft subscription blob endpoint for configs:  https://chaostest.blob.core.windows.net/config/azuresettings.json
        ///Zen3 subscription blob endpoint for configs: ==>  https://cmonkeylogs.blob.core.windows.net/configs/azuresettings.json
        /// Microsoft demo config file ==> https://stachaosteststorage.blob.core.windows.net/configs/azuresettings.json

        public const string ConfigEndpoint = "https://cmnewschema.blob.core.windows.net/configs/azuresettings.json";
    }
}
