using AzureChaos.Models;
using AzureChaos.Utilities;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.WindowsAzure.Storage;
using System;

namespace AzureChaos
{
    public class AzureClient
    {
        /// <summary>Get the Resource management client</summary>
        /// <returns>Returns the resource management object.</returns>
        public static IResourceManagementClient GetResourceManagementClient(ADConfiguration config)
        {
            try
            {
                AzureCredentials azureCredentials = AuthenticationHelper.GetAzureCredentials(config);
                if (azureCredentials == null)
                {
                    return null;
                }

                return new ResourceManagementClient(azureCredentials) { SubscriptionId = config.SubscriptionId};
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        /// <summary>Get the Azure object to get the all resources</summary>
        /// <returns>Returns the Azure object.</returns>
        public static IAzure GetAzure(ADConfiguration config)
        {
            IAzure azure = null;
            try
            {
                AzureCredentials azureCredentials = AuthenticationHelper.GetAzureCredentials(config);
                if (azureCredentials == null)
                {
                    return null;
                }

                /* Authentication options can be done as follows
                 *
                 *public static IAuthenticated Authenticate(RestClient restClient, string tenantId);
                 *public static IAuthenticated Authenticate(string authFile);
                 * public static IAuthenticated Authenticate(AzureCredentials azureCredentials); 
                 */

                azure = Azure
                   .Configure()
                   .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                   .Authenticate(azureCredentials)
                   .WithSubscription(config.SubscriptionId);
            }
            catch (Exception exception)
            {
                throw exception;
            }

            return azure;
        }
    }
}
