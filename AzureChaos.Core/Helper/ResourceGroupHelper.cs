using AzureChaos.Core.Models.Configs;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AzureChaos.Core.Helper
{
    public class ResourceGroupHelper
    {
        public static List<IResourceGroup> GetResourceGroupsInSubscription(IAzure azure, AzureSettings azureSettings)
        {
            var blackListedResourceGroupList = azureSettings.Chaos.BlackListedResourceGroups?.Split(',');
            var specifiedResourceGroups = azureSettings.Chaos.ResourceGroups;

            var resourceGroupList = azure.ResourceGroups.List();
            if(blackListedResourceGroupList == null)
            {
                return resourceGroupList.ToList();
            }

            var resourceGroups = resourceGroupList.Where(x => blackListedResourceGroupList.Contains(x.Name, StringComparer.OrdinalIgnoreCase));
            if (string.IsNullOrWhiteSpace(specifiedResourceGroups))
            {
                return resourceGroups.ToList();
            }

            var includedResourceGroups = specifiedResourceGroups.Split(',');
            if (includedResourceGroups != null && includedResourceGroups[0].Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                return resourceGroups.ToList();
            }

            resourceGroups = resourceGroups.Where(x => includedResourceGroups.Contains(x.Name, StringComparer.OrdinalIgnoreCase));
            return resourceGroups.ToList();
        }
    }
}