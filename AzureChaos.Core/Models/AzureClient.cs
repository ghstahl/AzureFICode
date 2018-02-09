using AzureChaos.Constants;
using AzureChaos.ReguestHelpers;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace AzureChaos.Models
{
    /// <summary>Azure configuration model which azure tenant, subscription
    /// and resource group needs to be crawled</summary>
    public class AzureClient
    {
        public IAzure azure;

        public AzureSettings azureSettings;
        public int MeanTimeInHours = 2;

        /// <summary>For now, keeping the configuration information here.</summary>
        /// will be adding the storage account details in the azure function and will provide azure function
        public AzureClient()
        {
            this.azureSettings = JsonConvert.DeserializeObject<AzureSettings>(HTTPHelpers.ExecuteGetWebRequest(Endpoints.ConfigEndpoint));
            this.azure = GetAzure(azureSettings.Client.ClientId, azureSettings.Client.ClientSecret, azureSettings.Client.TenantId, azureSettings.Client.SubscriptionId);
        }

        /// <summary>Get the Azure object to read the all resources from azure</summary>
        /// <returns>Returns the Azure object.</returns>
        private IAzure GetAzure(string clientId, string clientSecret, string tenantId, string subscriptionId)
        {
            IAzure azure = null;
            try
            {
                AzureCredentials azureCredentials = GetAzureCredentials(clientId, clientSecret, tenantId);
                if (azureCredentials == null)
                {
                    return null;
                }

                azure = Azure
                   .Configure()
                   .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                   .Authenticate(azureCredentials)
                   .WithSubscription(subscriptionId);
            }
            catch (Exception exception)
            {
                throw exception;
            }

            return azure;
        }

        /// <summary>Get azure credentials based on the client id and client secret.</summary>
        /// <returns></returns>
        private AzureCredentials GetAzureCredentials(string clientId, string clientSecret, string tenantId)
        {
            return SdkContext.AzureCredentialsFactory
                            .FromServicePrincipal(clientId, clientSecret, tenantId, AzureEnvironment.AzureGlobalCloud);
        }
    }
}
