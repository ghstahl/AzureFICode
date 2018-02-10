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
    public static class VirtualMachineScaleSetVMExecuter
    {
        /// <summary>Azure Configuration.</summary>
        private static readonly AzureClient AzureClient = new AzureClient();

        private static readonly IStorageAccountProvider StorageProvider = new StorageAccountProvider();
        private const string WarningMessageOnSameState = "Couldnot perform any chaos, since action type and initial state are same";
        private const string FunctionName = "scalesetvmexecuter";

        [FunctionName("scalesetvmexecuter")]
        public static async Task<bool> Run([OrchestrationTrigger] DurableOrchestrationContext context, TraceWriter log)
        {
            var input = context.GetInput<string>();
            InputObject inputObject;
            if (!ValidateInput(input, log, out inputObject))
            {
                return false;
            }

            var azureSettings = AzureClient.azureSettings;
            EventActivityEntity eventActivity = new EventActivityEntity(inputObject.ResourceGroup);
            var storageAccount = StorageProvider.CreateOrGetStorageAccount(AzureClient);
            try
            {
                IVirtualMachineScaleSetVM scaleSetVM = await GetVirtualMachineScaleSetVm(AzureClient.azure, inputObject, log);
                if(scaleSetVM == null)
                {
                    log.Info($"VM Scaleset Chaos : No resource found for the  scale set id: " + inputObject.ScalesetId);
                    return false;
                }
                
                log.Info($"VM ScaleSet Chaos received the action: " + inputObject.Action + " for the virtual machine: " + inputObject.ResourceName);

                SetInitialEventActivity(scaleSetVM, inputObject, eventActivity);

                // if its not valid chaos then update the event table with  warning message and return the bad request response
                bool isValidChaos = IsValidChaos(inputObject.Action, scaleSetVM.PowerState);
                if (!isValidChaos)
                {
                    log.Info($"VM ScaleSet- Invalid action: " + inputObject.Action);
                    eventActivity.Status = Status.Failed.ToString();
                    eventActivity.Warning = "Invalid Action";
                    await StorageProvider.InsertOrMergeAsync<EventActivityEntity>(eventActivity, storageAccount, azureSettings.ActivityLogTable);
                    return false;
                }

                eventActivity.Status = Status.Started.ToString();
                await StorageProvider.InsertOrMergeAsync<EventActivityEntity>(eventActivity, storageAccount, azureSettings.ActivityLogTable);
                await PerformChaos(inputObject.Action, scaleSetVM, eventActivity);
                scaleSetVM = await scaleSetVM.RefreshAsync();
                if (scaleSetVM != null)
                {
                    eventActivity.EventCompletedDate = DateTime.UtcNow;
                    eventActivity.FinalState = scaleSetVM.PowerState.Value;
                }

                await StorageProvider.InsertOrMergeAsync<EventActivityEntity>(eventActivity, storageAccount, azureSettings.ActivityLogTable);
                log.Info($"VM ScaleSet Chaos Completed");
                return true;
            }
            catch (Exception ex)
            {
                eventActivity.Error = ex.Message;
                eventActivity.Status = Status.Failed.ToString();
                await StorageProvider.InsertOrMergeAsync<EventActivityEntity>(eventActivity, storageAccount, azureSettings.ActivityLogTable);

                // dont throw the error here just handle the error and return the false
                log.Error($"VM ScaleSet Chaos trigger function threw the exception ", ex, FunctionName);
                log.Info($"VM ScaleSet Chaos Completed with error");
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
                if(string.IsNullOrWhiteSpace(inputObject.ScalesetId))
                {
                    log.Error("VMScaleset Id is not valid ");
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
            try
            {
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
            catch(Exception ex)
            {
                throw ex;
            }
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
        private static async Task<IVirtualMachineScaleSetVM> GetVirtualMachineScaleSetVm(IAzure azure, InputObject inputObject, TraceWriter log)
        {
            var vmScaleSet = await azure.VirtualMachineScaleSets.GetByIdAsync(inputObject.ScalesetId);
            if (vmScaleSet == null)
            {
                log.Info("VM Scaleset Chaos: scale set is returning null for the Id: " + inputObject.ScalesetId);
                return null;
            }

            var scaleSetVms = await vmScaleSet.VirtualMachines.ListAsync();
            if (scaleSetVms == null || !scaleSetVms.Any())
            {
                log.Info("VM Scaleset Chaos: scale set vm's are empty");
                return null;
            }

            return scaleSetVms.FirstOrDefault(x => x.Name.Equals(inputObject.ResourceName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
