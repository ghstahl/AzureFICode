using AzureChaos.Core.Utilities;
using AzureChaos.Enums;
using AzureChaos.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace AzureChaos.Controllers
{
    public class CrawlerController : ApiController
    {
        [HttpGet]
        public async void GetResourcesAsync()
        {
            var config = new ADConfiguration()
            {
                AuthenticationType = AuthenticationType.Credentials,
                ResourceGroup = "Chaos_Monkey_RG",
                SubscriptionId = "470546b8-4d7f-4c0e-ae30-489e29c7cb43",
                TenantId = "99b5d273-16d0-460f-8d7a-fa3cadd3913a",
                Region = "",
                ClientId = "f7ef7b09-6213-4b58-a207-7a90df389822",
                ClientSecret = "NDC93m7tV7/F6NbCX3gfbqSVeHK3DtxS+ggX11hbHKk="
            };
            var resources = await ResourceHelper.GetResources(config);
            if (resources == null)
            {
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }
            
        }
    }
}
