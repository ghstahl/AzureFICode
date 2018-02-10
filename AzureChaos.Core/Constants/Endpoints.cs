using System.Collections.Generic;

namespace AzureChaos.Constants
{
    public class Endpoints
    {

        public static Dictionary<string, string> FunctionNameMap = new Dictionary<string, string>()
        {
            { "Microsoft.Compute/virtualMachines", "vmexecuter" },
            { "Microsoft.Compute/virtualMachineScaleSets/virtualMachines", "scalesetvmexecuter" },
        };

        ///Microsoft subscription blob endpoint for configs:  https://chaostest.blob.core.windows.net/config/azuresettings.json
        ///Zen3 subscription blob endpoint for configs: ==>  https://cmonkeylogs.blob.core.windows.net/configs/azuresettings.json
        /// Microsoft demo config file ==> https://stachaosteststorage.blob.core.windows.net/configs/azuresettings.json

        public const string ConfigEndpoint = "https://cmnewschema.blob.core.windows.net/configs/azuresettings.json";
    }
}
