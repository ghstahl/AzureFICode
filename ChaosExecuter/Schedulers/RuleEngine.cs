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
            /// Scale Set Rule engine:
            IRuleEngine vmss = new ScaleSetRuleEngine();
            vmss.CreateRule(AzureClient);

            /// Scale Set Rule engine:
            IRuleEngine vm = new VirtualMachineRuleEngine();
            vm.CreateRule(AzureClient);

            //AvailabilityZone Rule Engine
            if (azureSettings.Chaos.AvailabilityZoneChaos.Enabled)
            {
                IRuleEngine availabilityZone = new AvailabilityZoneRuleEngine();
                availabilityZone.CreateRule(AzureClient);
            }
            if (azureSettings.Chaos.AvailabilitySetChaos.Enabled && (azureSettings.Chaos.AvailabilitySetChaos.FaultDomainEnabled || azureSettings.Chaos.AvailabilitySetChaos.UpdateDomainEnabled))
            {
                IRuleEngine availabilityset = new AvailabilitySetRuleEngine();
                availabilityset.CreateRule(AzureClient);

            }
            //dynamic data = Task.Run(req.Content.ReadAsAsync<InputObject>());
            var tas = Task.Run(() => req.Content.ReadAsAsync<InputObject>());
            var data = tas.Result;
            EventActivityEntity eventActivity = new EventActivityEntity(data.ResourceName);

            try
            {
                /*TODO Nithin - need to move all these code into individual file which is implements the ruleengine interface*/
                var storageAccount = StorageProvider.CreateOrGetStorageAccount(AzureClient);
                if (azureSettings.Chaos.AvailabilitySetChaos.Enabled && (azureSettings.Chaos.AvailabilitySetChaos.FaultDomainEnabled || azureSettings.Chaos.AvailabilitySetChaos.UpdateDomainEnabled))
                {
                    Random random = new Random();
                    bool flag = true;
                    CloudTable availabilitySetTable = await StorageProvider.CreateOrGetTableAsync(storageAccount, azureSettings.AvailabilitySetCrawlerTableName);
                    TableQuery<AvailabilitySetsCrawlerResponseEntity> availabilitySetTableQuery = new TableQuery<AvailabilitySetsCrawlerResponseEntity>().Where(TableQuery.CombineFilters(TableQuery.GenerateFilterConditionForDate("EntryInsertionTime", QueryComparisons.LessThanOrEqual, DateTimeOffset.UtcNow), TableOperators.And, TableQuery.GenerateFilterConditionForDate("EntryInsertionTime", QueryComparisons.GreaterThanOrEqual, DateTimeOffset.UtcNow.AddHours(-2))));
                    var resultsSet = availabilitySetTable.ExecuteQuery(availabilitySetTableQuery);
                    var availabilitySetOptions = resultsSet.Where(x => !string.IsNullOrWhiteSpace(x.Virtualmachines))
                        .Select(r => r.ResourceGroupName).ToList();
                    var randomAvailabilitySet = availabilitySetOptions[random.Next(0, availabilitySetOptions.Count - 1)];
                    if (azureSettings.Chaos.AvailabilitySetChaos.UpdateDomainEnabled)
                    {
                        var updateDomainNumber = resultsSet.Where(a => a.ResourceGroupName == randomAvailabilitySet)
                            .FirstOrDefault().UpdateDomainCount;
                        var randomUpdateDomain = random.Next(0, updateDomainNumber - 1);
                        CloudTable virtualMachineTable = await StorageProvider.CreateOrGetTableAsync(storageAccount, azureSettings.VirtualMachineCrawlerTableName);
                        var asQuery = TableQuery.GenerateFilterCondition("AS", QueryComparisons.Equal,
                            resultsSet.Where(a => a.ResourceGroupName == randomAvailabilitySet).FirstOrDefault().ResourceGroupName);
                        var uDQuery = TableQuery.GenerateFilterConditionForInt("UpdateDomain", QueryComparisons.Equal,
                            randomUpdateDomain);
                        var timeQuery = TableQuery.CombineFilters(TableQuery.GenerateFilterConditionForDate
                                ("EntryInsertionTime", QueryComparisons.LessThanOrEqual, DateTimeOffset.UtcNow),
                            TableOperators.And,
                            TableQuery.GenerateFilterConditionForDate("EntryInsertionTime",
                                QueryComparisons.GreaterThanOrEqual, DateTimeOffset.UtcNow.AddHours(-2)));
                        string finalQuery = TableQuery.CombineFilters(TableQuery.CombineFilters(asQuery, TableOperators.And, uDQuery), TableOperators.And, timeQuery);

                        TableQuery<VirtualMachineCrawlerResponseEntity> virtualMachinesTableQuery = new TableQuery<VirtualMachineCrawlerResponseEntity>()
                            .Where(finalQuery);
                        var vmResults = virtualMachineTable.ExecuteQuery(virtualMachinesTableQuery);

                        flag = false;
                    }
                    else if (azureSettings.Chaos.AvailabilitySetChaos.FaultDomainEnabled && flag)
                    {
                        var updateFaultNumber = resultsSet.Where(a => a.ResourceGroupName == randomAvailabilitySet)
                            .FirstOrDefault().FaultDomainCount;
                        var randomfaultDomain = random.Next(0, updateFaultNumber - 1);
                        CloudTable virtualMachineTable = await StorageProvider.CreateOrGetTableAsync(storageAccount, azureSettings.VirtualMachineCrawlerTableName);
                        var asQuery = TableQuery.GenerateFilterCondition("AS", QueryComparisons.Equal,
                            resultsSet.Where(a => a.ResourceGroupName == randomAvailabilitySet).FirstOrDefault().ResourceGroupName);
                        var uDQuery = TableQuery.GenerateFilterConditionForInt("FaultDomain", QueryComparisons.Equal,
                            randomfaultDomain);
                        var timeQuery = TableQuery.CombineFilters(TableQuery.GenerateFilterConditionForDate
                                ("EntryInsertionTime", QueryComparisons.LessThanOrEqual, DateTimeOffset.UtcNow),
                            TableOperators.And,
                            TableQuery.GenerateFilterConditionForDate("EntryInsertionTime",
                                QueryComparisons.GreaterThanOrEqual, DateTimeOffset.UtcNow.AddHours(-2)));
                        string finalQuery = TableQuery.CombineFilters(TableQuery.CombineFilters(asQuery, TableOperators.And, uDQuery), TableOperators.And, timeQuery);

                        TableQuery<VirtualMachineCrawlerResponseEntity> virtualMachinesTableQuery = new TableQuery<VirtualMachineCrawlerResponseEntity>()
                            .Where(finalQuery);
                        var vmResults = virtualMachineTable.ExecuteQuery(virtualMachinesTableQuery);
                    }
                }

                if (azureSettings.Chaos.AvailabilityZoneChaos.Enabled)
                {
                    Random random = new Random();
                    int randomAvailabilityZone = random.Next(1, 3);
                    string randomRegionName = AzureClient.azureSettings.Chaos.AvailabilityZoneChaos.Regions[
                        random.Next(0, AzureClient.azureSettings.Chaos.AvailabilityZoneChaos.Regions.Count() - 1)];
                    CloudTable virtualMachineTable = await StorageProvider.CreateOrGetTableAsync(storageAccount, azureSettings.VirtualMachineCrawlerTableName);
                    var azQuery = TableQuery.GenerateFilterConditionForInt("AvailabilityZone", QueryComparisons.Equal, randomAvailabilityZone);
                    var azRegionQuery =
                        TableQuery.GenerateFilterCondition("RegionName", QueryComparisons.Equal, randomRegionName);
                    var timeQuery = TableQuery.CombineFilters(TableQuery.GenerateFilterConditionForDate
                            ("EntryInsertionTime", QueryComparisons.LessThanOrEqual, DateTimeOffset.UtcNow),
                        TableOperators.And,
                        TableQuery.GenerateFilterConditionForDate("EntryInsertionTime",
                            QueryComparisons.GreaterThanOrEqual, DateTimeOffset.UtcNow.AddHours(-2)));
                    string finalQuery = TableQuery.CombineFilters(azQuery, TableOperators.And, timeQuery);
                    TableQuery<VirtualMachineCrawlerResponseEntity> availabilityZoneTableQuery = new TableQuery<VirtualMachineCrawlerResponseEntity>().Where(finalQuery);

                    var resultsSet = virtualMachineTable.ExecuteQuery(availabilityZoneTableQuery).Where(x => azureSettings.Chaos.AvailabilityZoneChaos.Regions.Contains(x.RegionName));
                    if (resultsSet.Count() > 0)
                    {
                        var sessionId = Guid.NewGuid().ToString();
                        TableBatchOperation scheduledRulesbatchOperation = VirtualMachineHelper.CreateScheduleEntity(resultsSet.ToList(), azureSettings.Chaos.SchedulerFrequency);
                        if (scheduledRulesbatchOperation.Count > 0)
                        {
                            CloudTable ruleEngineTable = await StorageProvider.CreateOrGetTableAsync(storageAccount, azureSettings.ScheduledRulesTable);
                            await ruleEngineTable.ExecuteBatchAsync(scheduledRulesbatchOperation);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }

            // Fetching the name from the path parameter in the request URL
            return req.CreateResponse(HttpStatusCode.OK, "Hello ");
        }
    }
}
