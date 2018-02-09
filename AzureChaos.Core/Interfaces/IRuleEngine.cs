using AzureChaos.Models;
using System.Threading.Tasks;

namespace AzureChaos.Interfaces
{
    public interface IRuleEngine
    {
        bool IsChaosEnabled(AzureClient azureClient);

        Task CreateRuleAsync(AzureClient azureClient);

        void CreateRule(AzureClient azureClient);
    }
}
