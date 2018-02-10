using AzureChaos.Entity;
using AzureChaos.Helper;
using AzureChaos.Interfaces;
using AzureChaos.Models;
using AzureChaos.Providers;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace ChaosExecuter.Schedulers
{
    public static class RuleEngine
    {
        private static readonly AzureClient AzureClient = new AzureClient();
        private static readonly IStorageAccountProvider StorageProvider = new StorageAccountProvider();

        [FunctionName("RuleEngine")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ruleengine")]HttpRequestMessage req, TraceWriter log)
        {
            if (req?.Content == null)
            {
                log.Info($"RuleEngine trigger function request parameter is empty.");
                return req.CreateResponse(HttpStatusCode.BadRequest, "Request is empty");
            }
            var azureSettings = AzureClient.azureSettings;
            log.Info("C# RuleEngine trigger function processed a request.");
            var randomSelectionProcess = RuleEngineHelper.PickRandomResource(StorageProvider, AzureClient);
            /// Scale Set Rule engine:
            if (randomSelectionProcess.isVirtualMachineScaleSet)
            {
                IRuleEngine vmss = new ScaleSetRuleEngine();
                vmss.CreateRule(AzureClient);
            }
            else if (randomSelectionProcess.isStandaloneVirtualMachine)
            {
                /// Scale Set Rule engine:
                IRuleEngine vm = new VirtualMachineRuleEngine();
                vm.CreateRule(AzureClient);
            }
            else if (randomSelectionProcess.isAvailabilityZone)
            {
                //AvailabilityZone Rule Engine
                if (azureSettings.Chaos.AvailabilityZoneChaos.Enabled)
                {
                    IRuleEngine availabilityZone = new AvailabilityZoneRuleEngine();
                    availabilityZone.CreateRule(AzureClient);
                }
            }
            else if (randomSelectionProcess.isAvailabilitySet)
            {

                if (azureSettings.Chaos.AvailabilitySetChaos.Enabled && (azureSettings.Chaos.AvailabilitySetChaos.FaultDomainEnabled || azureSettings.Chaos.AvailabilitySetChaos.UpdateDomainEnabled))
                {
                    IRuleEngine availabilityset = new AvailabilitySetRuleEngine();
                    availabilityset.CreateRule(AzureClient);

                }
            }
            return req.CreateResponse(HttpStatusCode.OK, "Hello ");
        }
    }
}
