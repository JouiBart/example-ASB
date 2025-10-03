using Azure.Messaging.ServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASB.Pusher
{
    internal static class AuthHelper
    {
        internal static ServiceBusClient GetServiceBusClient()
        {
            // Pro Managed Identity používáme pouze namespace, ne connection string
            var serviceBusNamespace = Environment.GetEnvironmentVariable("ServiceBusConnection__fullyQualifiedNamespace");
            if (string.IsNullOrEmpty(serviceBusNamespace))
            {
                throw new InvalidOperationException("ServiceBusConnection__fullyQualifiedNamespace environment variable is not set");
            }
            // Používáme Managed Identity pro autentizaci
            return new ServiceBusClient(serviceBusNamespace, new Azure.Identity.DefaultAzureCredential());
        }
    }
}
