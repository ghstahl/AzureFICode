using AzureChaos.Entity;
using AzureChaos.Models;
using AzureChaos.Providers;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AzureChaos.Interfaces
{
    public class AvailabilitySetRuleEngine : IRuleEngine
    {
        private IStorageAccountProvider storageAccountProvider;
        private CloudStorageAccount storageAccount;

        // mints here  --  60
        // getting resources from crawler tables, which was not in the activity table within the 60mints.
        // Random pick based on the configuration, and keeep those schedule time will be the same.
        //
        // add entries into the scheduler, along with action details - i.e. based on the final state of the resources,
        // ex. if it is Running, then action will PowerOff
        // with precondition check on the schedule table against that particular resource.

        public AvailabilitySetRuleEngine(AzureClient azureClient, IStorageAccountProvider storageAccountProvider, CloudStorageAccount storageAccount)
        {
            this.storageAccountProvider = storageAccountProvider;
            this.storageAccount = storageAccount;
        }

        public void CreateRule(AzureClient azureClient)
        {
            // Read resources from crawler table
            // check the configs
            // pick random
            throw new NotImplementedException();
        }

        public Task CreateRuleAsync(AzureClient azureClient)
        {
            throw new NotImplementedException();
        }

        public bool IsChaosEnabled(AzureClient azureClient)
        {
            if (azureClient == null || azureClient.azureSettings == null || azureClient.azureSettings.Chaos == null || !azureClient.azureSettings.Chaos.ChaosEnabled
                || azureClient.azureSettings.Chaos.AvailabilitySetChaos == null || !azureClient.azureSettings.Chaos.AvailabilitySetChaos.Enabled)
            {
                return false;
            }

            return true;
        }

        private async Task<IEnumerable<AvailabilitySetsCrawlerResponseEntity>> GetResourcesAsync(AzureClient azureClient)
        {
            if (storageAccount == null)
            {
                return null;
            }

            TableQuery<AvailabilitySetsCrawlerResponseEntity> query = new TableQuery<AvailabilitySetsCrawlerResponseEntity>();
            var filter = TableQuery.CombineFilters("Virtualmachines", QueryComparisons.NotEqual, string.Empty);
            return await this.storageAccountProvider.GetEntitiesAsync<AvailabilitySetsCrawlerResponseEntity>(query, this.storageAccount, azureClient.AvailabilitySetCrawlerTableName);
        }

        private IEnumerable<AvailabilitySetsCrawlerResponseEntity> GetResources(AzureClient azureClient)
        {
            if (storageAccount == null)
            {
                return null;
            }

            TableQuery<AvailabilitySetsCrawlerResponseEntity> query = new TableQuery<AvailabilitySetsCrawlerResponseEntity>();
            var filter = TableQuery.CombineFilters("Virtualmachines", QueryComparisons.NotEqual, string.Empty);
            var results = this.storageAccountProvider.GetEntities<AvailabilitySetsCrawlerResponseEntity>(query, this.storageAccount, azureClient.AvailabilitySetCrawlerTableName);
            return results;
        }
    }
}
