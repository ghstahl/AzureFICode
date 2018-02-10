using AzureChaos.Entity;
using AzureChaos.Enums;
using AzureChaos.Models;
using AzureChaos.Providers;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ChaosExecuter.Executer
{
    /// <summary>Virtual Machine chaos executer<see cref="VirtualMachineExecuter.cs"/></summary>
    public static class VirtualMachineExecuter
    {
        /// <summary>Azure Configuration.</summary>
        private static readonly AzureClient AzureClient = new AzureClient();

        private static readonly IStorageAccountProvider StorageProvider = new StorageAccountProvider();
        private const string WarningMessageOnSameState = "Couldnot perform any chaos, since action type and initial state are same";
        private const string FunctionName = "vmexecuter";

        /// <summary>Chaos executer on the Virtual Machines.</summary>
        /// <param name="req">The http request message.</param>
        /// <param name="log">The trace writer.</param>
        /// <returns>Returns the http response message.</returns>
        [FunctionName("vmexecuter")]
        public static async Task<bool> Run([OrchestrationTrigger] DurableOrchestrationContext context, TraceWriter log)
        {
            var input = context.GetInput<string>();
            InputObject inputObject;
            if (!ValidateInput(input, log, out inputObject))
            {
                return false;
            }

            var azureSettings = AzureClient.azureSettings;
            EventActivityEntity eventActivity = new EventActivityEntity(inputObject.ResourceName);
            var storageAccount = StorageProvider.CreateOrGetStorageAccount(AzureClient);
            try
            {
                IVirtualMachine virtualMachine = await GetVirtualMachine(AzureClient.azure, inputObject, log);
                if (virtualMachine == null)
                {
                    log.Info($"VM Chaos : No resource found for the resource name : " + inputObject.ResourceName);
                    return false;
                }

                log.Info($"VM Chaos received the action: " + inputObject.Action + " for the virtual machine: " + inputObject.ResourceName);

                /// checking the provisiong state before the starting the chaos, if its already in the failed or in-progress state, then we will not be doing any chaos.
                if (!Enum.TryParse(virtualMachine.ProvisioningState, out ProvisioningState provisioningState) && provisioningState != ProvisioningState.Succeeded)
                {
                    log.Info($"VM Chaos :  The vm '" + inputObject.ResourceName + "' is in the state of " + virtualMachine.ProvisioningState + ", so cannont perform the same action " + inputObject.Action);
                    eventActivity.Status = Status.Failed.ToString();
                    eventActivity.Error = "Vm provisioning state and action both are same, so couldnot perform the action";
                    await StorageProvider.InsertOrMergeAsync<EventActivityEntity>(eventActivity, storageAccount, azureSettings.ActivityLogTable);
                    return false;
                }

                SetInitialEventActivity(virtualMachine, inputObject, eventActivity);

                // if its not valid chaos then update the event table with  warning message and return false
                bool isValidChaos = IsValidChaos(inputObject.Action, virtualMachine.PowerState);
                if (!isValidChaos)
                {
                    log.Info($"VM Chaos- Invalid action: " + inputObject.Action);
                    eventActivity.Status = Status.Failed.ToString();
                    eventActivity.Warning = "Invalid Action";
                    await StorageProvider.InsertOrMergeAsync<EventActivityEntity>(eventActivity, storageAccount, azureSettings.ActivityLogTable);
                    return false;
                }

                eventActivity.Status = Status.Started.ToString();
                await StorageProvider.InsertOrMergeAsync<EventActivityEntity>(eventActivity, storageAccount, azureSettings.ActivityLogTable);
                await PerformChaos(inputObject.Action, virtualMachine, eventActivity);
                virtualMachine = await GetVirtualMachine(AzureClient.azure, inputObject, log);
                if (virtualMachine != null)
                {
                    eventActivity.EventCompletedDate = DateTime.UtcNow;
                    eventActivity.FinalState = virtualMachine.PowerState.Value;
                }

                await StorageProvider.InsertOrMergeAsync<EventActivityEntity>(eventActivity, storageAccount, azureSettings.ActivityLogTable);
                log.Info($"VM Chaos Completed");
                return true;
            }
            catch (Exception ex)
            {
                eventActivity.Error = ex.Message;
                eventActivity.Status = Status.Failed.ToString();
                await StorageProvider.InsertOrMergeAsync<EventActivityEntity>(eventActivity, storageAccount, azureSettings.ActivityLogTable);

                // dont throw the error here just handle the error and return the false
                log.Error($"VM Chaos trigger function threw the exception ", ex, FunctionName);
                log.Info($"VM Chaos Completed with error");
            }

            return false;
        }

        /// <summary>Validate the request input on this functions, and log the invalid.</summary>
        /// <param name="input"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        private static bool ValidateInput(string input, TraceWriter log, out InputObject inputObject)
        {
            try
            {
                inputObject = JsonConvert.DeserializeObject<InputObject>(input);
                if (inputObject == null)
                {
                    log.Error("input data is empty");
                    return false;
                }
                if (!Enum.TryParse(inputObject.Action.ToString(), out ActionType action))
                {
                    log.Error("Virtual Machine action is not valid action");
                    return false;
                }
                if (string.IsNullOrWhiteSpace(inputObject.ResourceName))
                {
                    log.Error("Virtual Machine Resource name is not valid name");
                    return false;
                }
                if (string.IsNullOrWhiteSpace(inputObject.ResourceGroup))
                {
                    log.Error("Virtual Machine Resource Group is not valid resource group");
                    return false;
                }
            }
            catch (Exception ex)
            {
                log.Error("Threw exception on the validate input method", ex, FunctionName + ": ValidateInput");
                inputObject = null;
                return false;
            }

            return true;
        }

        /// <summary>Perform the Chaos Operation</summary>
        /// <param name="actionType">Action type</param>
        /// <param name="virtualMachine">Virtual Machine</param>
        /// <param name="eventActivity">Event activity entity</param>
        /// <returns></returns>
        private static async Task PerformChaos(ActionType actionType, IVirtualMachine virtualMachine, EventActivityEntity eventActivity)
        {
            try
            {
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
            catch (Exception ex)
            {
                // throw the exception and handle it parent method.
                throw ex;
            }
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
        private static void SetInitialEventActivity(IVirtualMachine virtualMachine, InputObject data, EventActivityEntity eventActivity)
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
        private static async Task<IVirtualMachine> GetVirtualMachine(IAzure azure, InputObject inputObject, TraceWriter log)
        {
            var virtualMachines = await azure.VirtualMachines.ListByResourceGroupAsync(inputObject.ResourceGroup);
            if (virtualMachines == null || !virtualMachines.Any())
            {
                log.Info("No virtual machines are found in the resource group: " + inputObject.ResourceGroup);
                return null;
            }

            return virtualMachines.FirstOrDefault(x => x.Name.Equals(inputObject.ResourceName, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
