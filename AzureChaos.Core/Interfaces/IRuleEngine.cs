using AzureChaos.Models;
using System.Threading.Tasks;

namespace AzureChaos.Interfaces
{
    public interface IRuleEngine
    {
        bool IsChaosEnabled(AzureSettings azureSettings);

        Task CreateRuleAsync(AzureClient azureClient);

        void CreateRule(AzureClient azureClient);

        //TableBatchOperation CreateScheduleEntity<T>(IList<T> filteredSet) where T : ITableEntity;
    }
}