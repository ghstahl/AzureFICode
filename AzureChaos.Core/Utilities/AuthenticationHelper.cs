using AzureChaos.Enums;
using AzureChaos.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using System;
using System.Collections.Generic;
using System.Text;

namespace AzureChaos.Utilities
{
    public static class AuthenticationHelper
    {
        public static AzureCredentials GetAzureCredentials(ADConfiguration config)
        {
            if (!AuthenticationHelper.IsValidConfig(config))
            {
                return null;
            }

            AzureCredentials azureCredentials = null;
            if (config.AuthenticationType == AuthenticationType.Credentials) // will be adding other authenticaton types ex. by certificate
            {
                azureCredentials = SdkContext.AzureCredentialsFactory
                              .FromServicePrincipal(config.ClientId, config.ClientSecret, config.TenantId, AzureEnvironment.AzureGlobalCloud);
            }

            return azureCredentials;
        }


        public static bool IsValidConfig(ADConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("Azure configuration is null");
            }
            if (string.IsNullOrWhiteSpace(config.TenantId))
            {
                throw new ArgumentNullException("Please provide client id");
            }
            if (string.IsNullOrWhiteSpace(config.SubscriptionId))
            {
                throw new ArgumentNullException("Please provide the subscription id");
            }
            if (config.AuthenticationType == AuthenticationType.Credentials)
            {
                if (string.IsNullOrWhiteSpace(config.ClientId))
                {
                    throw new ArgumentNullException("Please provide client id");
                }
                if (string.IsNullOrWhiteSpace(config.ClientSecret))
                {
                    throw new ArgumentNullException("Please provide client secret");
                }
            }
            else if (config.AuthenticationType == AuthenticationType.Certificate)
            {
                if (string.IsNullOrWhiteSpace(config.CertificatePath))
                {
                    throw new ArgumentNullException("Please provide certificate path");
                }
                if (string.IsNullOrWhiteSpace(config.CertificatePassword))
                {
                    throw new ArgumentNullException("Please provide certificate password");
                }
            }

            return true;
        }
    }
}
