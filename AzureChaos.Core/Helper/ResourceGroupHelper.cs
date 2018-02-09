using Microsoft.Azure.Management.Fluent;
using System.Collections.Generic;

namespace AzureChaos.Helper
{
    public class ResourceGroupHelper
    {
        public static List<string> GetResourceGroupsInSubscription(IAzure azure)
        {
            List<string> resourceGroups = new List<string>();
            var resourceGroupList = azure.ResourceGroups.List();
            foreach (var resourceGroup in resourceGroupList)
            {
                resourceGroups.Add(resourceGroup.Name);
                // Add a condition to check a RG in BL
            }
            return resourceGroups;
        }
    }
}