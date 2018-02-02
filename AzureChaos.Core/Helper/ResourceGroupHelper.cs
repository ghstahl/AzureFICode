using AzureChaos.Models;
using System.Collections.Generic;

namespace AzureChaos.Helper
{
    public class ResourceGroupHelper
    {
        public static List<string> GetResourceGroupsInSubscription(ADConfiguration config)
        {
            List<string> resourceGroups = new List<string>();
            var azure_client = AzureClient.GetAzure(config);   //.GetAzure(config);
            var resourceGroupList = azure_client.ResourceGroups.List();
            foreach (var resourceGroup in resourceGroupList)
            {
                resourceGroups.Add(resourceGroup.Name);
                // Add a condition to check a RG in BL
            }
            return resourceGroups;
        }
    }
}
