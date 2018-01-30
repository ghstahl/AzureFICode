using AzureChaos;
using AzureChaos.Entity;
using AzureChaos.Enums;
using AzureChaos.Models;
using AzureChaos.Providers;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace VirtualMachineChaosExecuter
{
    /// <summary>Virtual Machine chaos executer<see cref="VMChaos.cs"/></summary>
    public static class VMChaos
    {
        /// <summary>Azure Configuration.</summary>
        private static ADConfiguration config = new ADConfiguration();
        private static StorageAccountProvider storageProvider = new StorageAccountProvider("st5ce22456", "KoCL+uvZZCpsuROXas0cGdm8jJCYurb5OiaNNhlNTzzl0oBhjoni72gryV70YwaO/sYXexhzG0ZcexQWhPTmdA==");
        private static string eventTableName = "dummytablename";

        /// <summary>Chaos executer on the Virtual Machines.</summary>
        /// <param name="req">The http request message.</param>
        /// <param name="log">The trace writer.</param>
        /// <returns>Returns the http response message.</returns>
        [FunctionName("vmchaos")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            if (req == null || req.Content == null)
            {
                log.Info($"VM Chaos trigger function request parameter is empty.");
                return req.CreateResponse(HttpStatusCode.BadRequest, "Request is empty");
            }

            log.Info($"VM Chaos trigger function processed a request. RequestUri= { req.RequestUri }");
            // Get request body
            dynamic data = await req.Content.ReadAsAsync<InputObject>();
            ActionType action;
            if (data == null || !Enum.TryParse(data?.Action.ToString(), out action) || string.IsNullOrWhiteSpace(data?.ResourceName))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid Action/Resource");
            }

            EventActivity eventActivity = new EventActivity(data.ResourceName);
            try
            {
                IVirtualMachine virtualMachine = GetVirtualMachine(config, config.ResourceGroup, data.ResourceName);
                ///GetByResourceGroup is throwing exception on the virtual machine. using alternative method to get the resources.
                //    IVirtualMachine virtualMachine = azure.VirtualMachines.GetByResourceGroup(config.ResourceGroup, data?.ResourceName);
                var storageAccount = storageProvider.CreateStorageAccountIfNotExist(config);
                if (storageAccount == null)
                {
                    return req.CreateResponse(HttpStatusCode.InternalServerError, "storage account not created/not existed");
                }

                log.Info($"VM Chaos trigger function Processing the action= " + data?.Action);
                eventActivity.InitialState = virtualMachine.PowerState.Value;
                eventActivity.Resource = data.ResourceName;
                eventActivity.ResourceType = ResourceType.VirtualMachines.ToString();
                eventActivity.ResourceGroup = config.ResourceGroup;
                eventActivity.EventType = data.Action.ToString();
                eventActivity.EventStateDate = DateTime.UtcNow;
                eventActivity.EntryDate = DateTime.UtcNow;
                storageProvider.InsertEntity(eventActivity, eventTableName);
                switch (data.Action)
                {
                    case ActionType.Start:
                        if (virtualMachine.PowerState != PowerState.Running && virtualMachine.PowerState != PowerState.Starting)
                        {
                            virtualMachine.Start();
                        }

                        break;
                    case ActionType.PowerOff:
                    case ActionType.Stop:
                        if (virtualMachine.PowerState != PowerState.Stopping && virtualMachine.PowerState != PowerState.Stopped)
                        {
                            virtualMachine.PowerOff();
                        }

                        break;
                }

                virtualMachine = GetVirtualMachine(config, config.ResourceGroup, data?.ResourceName);
                EventActivity result = await storageProvider.GetSingleEntity<EventActivity>(eventActivity.PartitionKey, eventActivity.RowKey, eventTableName);
                if (result != null)
                {
                    result.EventCompletedDate = DateTime.UtcNow;
                    result.FinalState = virtualMachine.PowerState.Value;
                    result.Error = virtualMachine.PowerState.Value.Equals(result.InitialState, StringComparison.InvariantCultureIgnoreCase) ? "Couldnot perform any chaos, since action type and initial state are same": string.Empty;
                    await storageProvider.InsertOrMerge<EventActivity>(result, eventTableName);
                }

                return req.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                eventActivity.Error = ex.Message;
                storageProvider.InsertEntity(eventActivity, eventTableName);
                log.Error($"VM Chaos trigger function Throw the exception ", ex, "VMChaos");
                return req.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        /// <summary>Get the virtual machine.</summary>
        /// <param name="config">The config</param>
        /// <param name="resourceGroup">The Resource Group.</param>
        /// <param name="resourceName">The Resource Name</param>
        /// <returns>Returns the virtual machine.</returns>
        private static IVirtualMachine GetVirtualMachine(ADConfiguration config, string resourceGroup, string resourceName)
        {
            var azure = AzureClient.GetAzure(config);
            var virtualMachines = azure.VirtualMachines.ListByResourceGroup(config.ResourceGroup);
            if (virtualMachines == null || !virtualMachines.Any())
            {
                return null;
            }

            var virtualMachine = virtualMachines.FirstOrDefault(x => x.Name.Equals(resourceName, StringComparison.InvariantCultureIgnoreCase));
            if (virtualMachine == null)
            {
                return null;
            }

            return virtualMachine;
        }
    }
}