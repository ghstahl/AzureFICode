using System.Collections.Generic;

namespace AzureChaos.Constants
{
    public class Endpoints
    {
        public static Dictionary<string, string> ComputeExecuterEndpoints = new Dictionary<string, string>()
        {
            { "Microsoft.Compute/virtualMachines", "https://microsoftchaosdemo.azurewebsites.net/api/vmexecuter?code=CiLRE7vjHKhxYea27KH6Ca0TEt7JftQTAm5e1f7VKG0whVPUuF23fw==" },
            { "Microsoft.Compute/virtualMachineScaleSets/virtualMachines", "https://microsoftchaosdemo.azurewebsites.net/api/scalesetvmexecuter?code=7IOsdnBIbaj0w8clyuqkRTlGX9BTUt6WeUm2kGc80S2u/Teayksh4w==" },
        };

        ///Microsoft subscription blob endpoint for configs:  https://chaostest.blob.core.windows.net/config/azuresettings.json
        ///Zen3 subscription blob endpoint for configs: ==>  https://cmonkeylogs.blob.core.windows.net/configs/azuresettings.json
        /// Microsoft demo config file ==> https://stachaosteststorage.blob.core.windows.net/configs/azuresettings.json

        public const string ConfigEndpoint = "https://cmonkeylogs.blob.core.windows.net/configs/azuresettings.json";
    }
}
