using AzureChaos.Core.Constants;
using AzureChaos.Core.Entity;
using AzureChaos.Core.Enums;
using AzureChaos.Core.Helper;
using AzureChaos.Core.Models;
using AzureChaos.Core.Providers;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table.Protocol;

namespace AzureChaos.Core.Interfaces
{
    public class LoadBalancerRuleEngine : IRuleEngine
    {
        private readonly AzureClient _azureClient = new AzureClient();
        public void CreateRule(TraceWriter log)
        {

        }
    }
}
