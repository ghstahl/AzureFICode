using AzureChaos;
using AzureChaos.Entity;
using AzureChaos.Enums;
using AzureChaos.Models;
using AzureChaos.Providers;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace ChaosExecuter.Executer
{
    /// <summary>Virtual Machine chaos executer<see cref="VirtualMachineExecuter.cs"/></summary>
    public static class VirtualMachineExecuter
    {
        /// <summary>Azure Configuration.</summary>
        private static ADConfiguration config = new ADConfiguration();

        private static StorageAccountProvider storageProvider = new StorageAccountProvider(config);
        private static string eventTableName = "dummytablename";
        private const string WarningMessageOnSameState = "Couldnot perform any chaos, since action type and initial state are same";

        /// <summary>Chaos executer on the Virtual Machines.</summary>
        /// <param name="req">The http request message.</param>
        /// <param name="log">The trace writer.</param>
        /// <returns>Returns the http response message.</returns>
        [FunctionName("vmchaos")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "vmexecuter")]HttpRequestMessage req, TraceWriter log)
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
            if (data == null || !Enum.TryParse(data?.Action.ToString(), out action) || string.IsNullOrWhiteSpace(data?.ResourceName) || string.IsNullOrWhiteSpace(data?.ResourceGroup))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid Action/Resource/ResourceGroup");
            }

            EventActivityEntity eventActivity = new EventActivityEntity(data.ResourceName);
            try
            {
                var azure = AzureClient.GetAzure(config);
                IVirtualMachine virtualMachine = await GetVirtualMachine(azure, config.ResourceGroup, data.ResourceName);
                log.Info($"VM Chaos trigger function Processing the action= " + data?.Action);

                SetInitialEventActivity(virtualMachine, data, eventActivity);

                // if its not valid chaos then update the event table with  warning message and return the bad request response
                bool isValidChaos = IsValidChaos(data.Action, virtualMachine.PowerState);
                if (!isValidChaos)
                {
                    eventActivity.Status = Status.Failed.ToString();
                    eventActivity.Warning = WarningMessageOnSameState;
                    await storageProvider.InsertOrMerge<EventActivityEntity>(eventActivity, eventTableName);
                    return req.CreateResponse(HttpStatusCode.BadRequest);
                }

                await PerformChaos(data.Action, virtualMachine, eventActivity);
                virtualMachine = await GetVirtualMachine(azure, config.ResourceGroup, data.ResourceName); // virtualMachine.RefreshAsync(); 
                if (virtualMachine != null)
                {
                    eventActivity.EventCompletedDate = DateTime.UtcNow;
                    eventActivity.FinalState = virtualMachine.PowerState.Value;
                }

                await storageProvider.InsertOrMerge<EventActivityEntity>(eventActivity, eventTableName);
                return req.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                eventActivity.Error = ex.Message;
                await storageProvider.InsertOrMerge<EventActivityEntity>(eventActivity, eventTableName);
                log.Error($"VM Chaos trigger function Throw the exception ", ex, "VMChaos");
                return req.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        /// <summary>Perform the Chaos Operation</summary>
        /// <param name="actionType">Action type</param>
        /// <param name="virtualMachine">Virtual Machine</param>
        /// <param name="eventActivity">Event activity entity</param>
        /// <returns></returns>
        private static async Task PerformChaos(ActionType actionType, IVirtualMachine virtualMachine, EventActivityEntity eventActivity)
        {
            eventActivity.Status = Status.Started.ToString();
            await storageProvider.InsertOrMerge<EventActivityEntity>(eventActivity, eventTableName);
            switch (actionType)
            {
                case ActionType.Start:
                    await virtualMachine.StartAsync();
                    break;
                case ActionType.PowerOff:
                case ActionType.Stop:
                    await virtualMachine.PowerOffAsync();
                    break;
                case ActionType.Restart:
                    await virtualMachine.RestartAsync();
                    break;
            }

            eventActivity.Status = Status.Completed.ToString();
        }

        /// <summary>Check the given action is valid chaos to perform on the vm</summary>
        /// <param name="currentAction">Current request action</param>
        /// <param name="state">Current Vm state.</param>
        /// <returns></returns>
        private static bool IsValidChaos(ActionType currentAction, PowerState state)
        {
            if (currentAction == ActionType.Start)
            {
                return state != PowerState.Running && state != PowerState.Starting;
            }
            else if (currentAction == ActionType.Stop || currentAction == ActionType.PowerOff || currentAction == ActionType.Restart)
            {
                return state != PowerState.Stopping && state != PowerState.Stopped;
            }
            else
            {
                return false;
            }
        }

        /// <summary>Set the initial property of the activity entity</summary>
        /// <param name="virtualMachine">The vm</param>
        /// <param name="data">Request</param>
        /// <param name="eventActivity">Event activity entity.</param>
        private static void SetInitialEventActivity(IVirtualMachine virtualMachine, dynamic data, EventActivityEntity eventActivity)
        {
            eventActivity.InitialState = virtualMachine.PowerState.Value;
            eventActivity.Resource = data.ResourceName;
            eventActivity.ResourceType = virtualMachine.Type;
            eventActivity.ResourceGroup = data.ResourceGroup;
            eventActivity.EventType = data.Action.ToString();
            eventActivity.EventStateDate = DateTime.UtcNow;
            eventActivity.EntryDate = DateTime.UtcNow;
        }

        /// <summary>Get the virtual machine.</summary>
        /// <param name="config">The config</param>
        /// <param name="resourceGroup">The Resource Group.</param>
        /// <param name="resourceName">The Resource Name</param>
        /// <returns>Returns the virtual machine.</returns>
        private static async Task<IVirtualMachine> GetVirtualMachine(IAzure azure, string resourceGroup, string resourceName)
        {
            var virtualMachines = await azure.VirtualMachines.ListByResourceGroupAsync(config.ResourceGroup);
            if (virtualMachines == null || !virtualMachines.Any())
            {
                return null;
            }

            var virtualMachine = virtualMachines.FirstOrDefault(x => x.Name.Equals(resourceName, StringComparison.InvariantCultureIgnoreCase));
            //if (virtualMachine == null)
            //{
            //    return null;
            //}

            return virtualMachine;
        }
    }

}