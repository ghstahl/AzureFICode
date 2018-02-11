using AzureChaos.Models;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host;

namespace AzureChaos.Interfaces
{
    public interface IRuleEngine
    {
        Task CreateRuleAsync(AzureClient azureClient);

        void CreateRule(AzureClient azureClient, TraceWriter log);

        //TableBatchOperation CreateScheduleEntity<T>(IList<T> filteredSet) where T : ITableEntity;
    }
}