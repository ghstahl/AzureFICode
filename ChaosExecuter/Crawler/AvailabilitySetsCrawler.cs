using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AzureChaos;
using AzureChaos.Entity;
using AzureChaos.Enums;
using AzureChaos.Models;
using AzureChaos.Providers;
using ChaosExecuter.Helper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;

namespace ChaosExecuter.Crawler
{
    public static class AvailabilitySetsCrawler
    {
        private static ADConfiguration config = new ADConfiguration();
        private static StorageAccountProvider storageProvider = new StorageAccountProvider(config);
        private static CloudTableClient tableClient = storageProvider.tableClient;
        private static CloudTable availabilitySetTable = storageProvider.CreateOrGetTable("AvailabilitySetsTable");
        private static CloudTable vmTable = storageProvider.CreateOrGetTable("VirtualMachineTable");

        [FunctionName("AvailabilitySetsCrawler")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "CrawlAvailableSets")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");
            if (req == null || req.Content == null)
            {
                log.Info($"VM Chaos trigger function request parameter is empty.");
                return req.CreateResponse(HttpStatusCode.BadRequest, "Request is empty");
            }

            log.Info($"VM Chaos trigger function processed a request. RequestUri= { req.RequestUri }");
            // Get request body
            dynamic data = await req.Content.ReadAsAsync<InputObject>();
            //if (data == null || string.IsNullOrWhiteSpace(data?.ResourceName))
            //{
            //    // return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid ResourceGroup");
            //}
            try
            {
                TableBatchOperation availableSetbatchOperation = new TableBatchOperation();
                TableBatchOperation vmBatchOperation = new TableBatchOperation();
                var azure_client = AzureClient.GetAzure(config);
                var availability_sets = azure_client.AvailabilitySets.List();
                foreach (var availabilitySet in availability_sets)
                {
                    AvailabilitySetsCrawlerResponseEntity availabilitySetsCrawlerResponseEntity = new AvailabilitySetsCrawlerResponseEntity("CrawlRGs", Guid.NewGuid().ToString());
                    try
                    {
                        availabilitySetsCrawlerResponseEntity.EntryInsertionTime = DateTime.Now;
                        availabilitySetsCrawlerResponseEntity.Id = availabilitySet.Id;
                        availabilitySetsCrawlerResponseEntity.RegionName = availabilitySet.RegionName;
                        availabilitySetsCrawlerResponseEntity.ResourceGroupName = availabilitySet.Name;
                        availabilitySetsCrawlerResponseEntity.FaultDomainCount = availabilitySet.FaultDomainCount;
                        availabilitySetsCrawlerResponseEntity.UpdateDomainCount = availabilitySet.UpdateDomainCount;
                        if (availabilitySet.Inner.VirtualMachines.Count != 0)
                        {
                            if (availabilitySet.Inner.VirtualMachines.Count > 0)
                            {
                                List<string> vmIds = new List<string>();
                                foreach (var vm_in_as in availabilitySet.Inner.VirtualMachines)
                                {
                                    vmIds.Add(vm_in_as.Id.Split('/')[8]);
                                }
                                availabilitySetsCrawlerResponseEntity.Virtualmachines = string.Join(",", vmIds);
                            }
                        }

                        var virtualMachines = azure_client.VirtualMachines.ListByResourceGroup(config.ResourceGroup).Where(x => x.AvailabilitySetId == availabilitySet.Id);
                        foreach (var vm in virtualMachines)
                        {
                            var vmEntity = VirtualMachineHelper.ConvertToVirtualMachineEntity(vm, config.ResourceGroup);
                            vmEntity.VirtualMachineGroup = VirtualMachineGroup.AvailabilitySets.ToString();
                            vmBatchOperation.Insert(vmEntity);
                        }

                        availableSetbatchOperation.Insert(availabilitySetsCrawlerResponseEntity);
                    }
                    catch (Exception ex)
                    {
                        availabilitySetsCrawlerResponseEntity.Error = ex.Message;
                        log.Error($"VM Chaos trigger function Throw the exception ", ex, "VMChaos");
                        return req.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
                    }
                }

                await Task.Factory.StartNew(() =>
                {
                    availabilitySetTable.ExecuteBatch(availableSetbatchOperation);
                    vmTable.ExecuteBatch(vmBatchOperation);
                });
            }
            catch
            {
            }

            // Fetching the name from the path parameter in the request URL
            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}