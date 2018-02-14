using AzureChaos.Core.Models;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;

namespace AzureChaos.Core.Interfaces
{
    public interface IRuleEngine
    {
        Task CreateRuleAsync(AzureClient azureClient);

        void CreateRule(AzureClient azureClient, TraceWriter log);

        //TableBatchOperation CreateScheduleEntity<T>(IList<T> filteredSet) where T : ITableEntity;
    }
}