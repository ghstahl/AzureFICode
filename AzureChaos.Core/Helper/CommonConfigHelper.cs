using AzureChaos.Providers;
using System;
using System.Collections.Generic;
using System.Text;

namespace AzureChaos.Helper
{
    public class CommonConfigHelper
    {
        private static readonly IStorageAccountProvider StorageProvider = new StorageAccountProvider();
        public static string GetBlobData()
        { 
            return "Hi";
        }
    }
}
