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
        private static StorageAccountProvider storageProvider = new StorageAccountProvider();
        private static string eventTableName = "dummytablename";

        /// <summary>Chaos executer on the Virtual Machines.</summary>
        /// <param name="req">The http request message.</param>
        /// <param name="log">The trace writer.</param>
        /// <returns>Returns the http response message.</returns>
        [FunctionName("vmchaos")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function,"post", Route = "CreateChaos")]HttpRequestMessage req, TraceWriter log)
        {
            if (req == null || req.Content == null)
            {
                log.Info($"VM Chaos trigger function request parameter is empty.");
                return req.CreateResponse(HttpStatusCode.BadRequest, "Request is empty");
            }

            log.Info($"VM Chaos trigger function processed a request. RequestUri= { req.RequestUri }");
            // Get request body
            dynamic data = await req.Content.ReadAsAsync<InputObject>();
            if (data == null)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Action and Resource name on the body");
            }

            EventActivity eventActivity = new EventActivity(ResourceType.VirtualMachines.ToString(), data?.ResourceName);
            try
            {
                var azure = AzureClient.GetAzure(config);
                var virtualMachine = await azure.VirtualMachines.GetByResourceGroupAsync(config.ResourceGroup, data?.ResourceName);
                var storageAccount = storageProvider.CreateStorageAccountIfNotExist(config);
                if (storageAccount == null)
                {
                    return req.CreateResponse(HttpStatusCode.InternalServerError, "storage account not created/not existed");
                }

                log.Info($"VM Chaos trigger function Processing the action= " + data?.Action);
                Task vmTask = Task.Run(async () =>
                {
                    State initialState;
                    eventActivity.InitialState = Enum.TryParse(virtualMachine.PowerState.ToString(), out initialState) ? initialState : State.Unknown;
                    eventActivity.Resource = data?.ResourceName;
                    eventActivity.ResourceType = ResourceType.VirtualMachines;
                    eventActivity.ResourceGroup = config.ResourceGroup;
                    eventActivity.EventType = data?.Action;
                    eventActivity.EventStateDate = DateTime.UtcNow;
                    eventActivity.EntryDate = DateTime.UtcNow;
                    switch (data?.Action)
                    {
                        case ActionType.Start:
                            if (virtualMachine.PowerState != PowerState.Running && virtualMachine.PowerState != PowerState.Starting)
                            {
                                await virtualMachine.StartAsync();
                            }

                            break;
                        case ActionType.PowerOff:
                            if (virtualMachine.PowerState != PowerState.Stopping && virtualMachine.PowerState != PowerState.Stopped)
                            {
                                await virtualMachine.PowerOffAsync();
                            }

                            break;
                        case ActionType.Restart:
                            await virtualMachine.RestartAsync();
                            break;
                        default:
                            break;
                    }
                });

                if (vmTask.IsCompleted)
                {
                    eventActivity.EntryDate = DateTime.UtcNow;
                    virtualMachine = await azure.VirtualMachines.GetByResourceGroupAsync(config.ResourceGroup, data?.ResourceName);
                    State finalState;
                    eventActivity.FinalState = Enum.TryParse(virtualMachine.PowerState.ToString(), out finalState) ? finalState : State.Unknown;
                }
            }
            catch (Exception ex)
            {
                eventActivity.Error = ex.Message;
                log.Error($"VM Chaos trigger function Throw the exception ", ex, "VMChaos");
                return req.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }

            storageProvider.InsertEntity(eventActivity, eventTableName);
            return data == null
                    ? req.CreateResponse(HttpStatusCode.BadRequest, "Missing Action and Resource name on the body")
                    : req.CreateResponse(HttpStatusCode.OK);
        }
    }
}