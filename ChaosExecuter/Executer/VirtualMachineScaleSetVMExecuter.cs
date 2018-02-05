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
    public static class VirtualMachineScaleSetVMExecuter
    {
        /// <summary>Azure Configuration.</summary>
        private static AzureClient azureClient = new AzureClient();
        private static StorageAccountProvider storageProvider = new StorageAccountProvider();
        private const string WarningMessageOnSameState = "Couldnot perform any chaos, since action type and initial state are same";

        [FunctionName("scalesetvmexecuter")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "scalesetvmexecuter")]HttpRequestMessage req, TraceWriter log)
        {
            if (req == null || req.Content == null)
            {
                log.Info($"VMScaleSet Chaos trigger function request parameter is empty.");
                return req.CreateResponse(HttpStatusCode.BadRequest, "Request is empty");
            }

            log.Info($"VMScaleSet Chaos trigger function processed a request. RequestUri= { req.RequestUri }");
            // Get request body
            dynamic data = await req.Content.ReadAsAsync<InputObject>();
            ActionType action;
            if (data == null || !Enum.TryParse(data?.Action.ToString(), out action) || string.IsNullOrWhiteSpace(data?.ResourceName)
                || string.IsNullOrWhiteSpace(data?.ResourceGroup) || string.IsNullOrWhiteSpace(data?.ScaleSetName))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid Action/Resource/ResourceGroup/ScalesetName");
            }

            EventActivityEntity eventActivity = new EventActivityEntity(data.ResourceGroup);
            try
            {
                IVirtualMachineScaleSetVM scaleSetVM = await GetVirtualMachineScaleSetVm(azureClient.azure, data.ResourceGroup, data.ResourceName, data.ScaleSetName);
                log.Info($"VMScaleSet Chaos trigger function Processing the action= " + data.Action);
                SetInitialEventActivity(scaleSetVM, data, eventActivity);

                // if its not valid chaos then update the event table with  warning message and return the bad request response
                bool isValidChaos = IsValidChaos(data.Action, scaleSetVM.PowerState);
                if (!isValidChaos)
                {
                    eventActivity.Status = Status.Failed.ToString();
                    eventActivity.Warning = WarningMessageOnSameState;
                    await storageProvider.InsertOrMerge<EventActivityEntity>(eventActivity, azureClient.ActivityLogTable);
                    return req.CreateResponse(HttpStatusCode.BadRequest);
                }

                await PerformChaos(data.Action, scaleSetVM, eventActivity);
                scaleSetVM = await scaleSetVM.RefreshAsync();
                if (scaleSetVM != null)
                {
                    eventActivity.EventCompletedDate = DateTime.UtcNow;
                    eventActivity.FinalState = scaleSetVM.PowerState.Value;
                }

                await storageProvider.InsertOrMerge<EventActivityEntity>(eventActivity, azureClient.ActivityLogTable);
                return req.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                eventActivity.Error = ex.Message;
                await storageProvider.InsertOrMerge<EventActivityEntity>(eventActivity, azureClient.ActivityLogTable);
                log.Error($"VMScaleSet Chaos trigger function Throw the exception ", ex, "VMChaos");
                return req.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        /// <summary>Check the given action is valid chaos to perform on the scale set vm</summary>
        /// <param name="currentAction">Current request action</param>
        /// <param name="state">Current scale set Vm state.</param>
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

        /// <summary>Perform the Chaos Operation</summary>
        /// <param name="actionType">Action type</param>
        /// <param name="virtualMachine">Virtual Machine</param>
        /// <param name="eventActivity">Event activity entity</param>
        /// <returns></returns>
        private static async Task PerformChaos(ActionType actionType, IVirtualMachineScaleSetVM scaleSetVm, EventActivityEntity eventActivity)
        {
            eventActivity.Status = Status.Started.ToString();
            await storageProvider.InsertOrMerge<EventActivityEntity>(eventActivity, azureClient.ActivityLogTable);
            switch (actionType)
            {
                case ActionType.Start:
                    await scaleSetVm.StartAsync();
                    break;
                case ActionType.PowerOff:
                case ActionType.Stop:
                    await scaleSetVm.PowerOffAsync();
                    break;
                case ActionType.Restart:
                    await scaleSetVm.RestartAsync();
                    break;
            }

            eventActivity.Status = Status.Completed.ToString();
        }

        /// <summary>Set the initial property of the activity entity</summary>
        /// <param name="scaleSetVm">The vm</param>
        /// <param name="data">Request</param>
        /// <param name="eventActivity">Event activity entity.</param>
        private static void SetInitialEventActivity(IVirtualMachineScaleSetVM scaleSetVm, dynamic data, EventActivityEntity eventActivity)
        {
            eventActivity.InitialState = scaleSetVm.PowerState.Value;
            eventActivity.Resource = data.ResourceName;
            eventActivity.ResourceType = scaleSetVm.Type;
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
        private static async Task<IVirtualMachineScaleSetVM> GetVirtualMachineScaleSetVm(IAzure azure, string resourceGroup, string resourceName, string scaleSet)
        {
            var vmScaleSets = await azure.VirtualMachineScaleSets.ListByResourceGroupAsync(resourceGroup);
            if (vmScaleSets == null || !vmScaleSets.Any())
            {
                return null;
            }

            var scaleSetInstance = vmScaleSets.FirstOrDefault(x => x.Name.Equals(scaleSet, StringComparison.OrdinalIgnoreCase));
            if (scaleSetInstance == null)
            {
                return null;
            }

            var scaleSetVms = await scaleSetInstance.VirtualMachines.ListAsync();//.FirstOrDefault(x => x.Name.Equals(resourceName, StringComparison.InvariantCultureIgnoreCase));
            if (scaleSetVms == null || !scaleSetVms.Any())
            {
                return null;
            }

            return scaleSetVms.FirstOrDefault(x => x.Name.Equals(resourceName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
