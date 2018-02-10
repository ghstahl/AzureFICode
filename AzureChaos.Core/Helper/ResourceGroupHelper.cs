using Microsoft.Azure.Management.Fluent;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections.Generic;

namespace AzureChaos.Helper
{
    public class ResourceGroupHelper
    {
        public static List<string> GetResourceGroupsInSubscription(IAzure azure, string blackListedResourceGroups)
        {
            var blackListedResourceGroupList = blackListedResourceGroups.Split(',');
            List<string> resourceGroups = new List<string>();
            var resourceGroupList = azure.ResourceGroups.List();
            foreach (var resourceGroup in resourceGroupList)
            {
                if (!blackListedResourceGroupList.Contains(resourceGroup.Name, StringComparer.OrdinalIgnoreCase))
                {
                    resourceGroups.Add(resourceGroup.Name);
                }
                // Add a condition to check a RG in BL
            }
            return resourceGroups;
        }
    }
}