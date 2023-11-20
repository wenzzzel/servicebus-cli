using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace servicebus_cli.Repositories;

public interface IServiceBusRepository
{
    ServiceBusAdministrationClient GetServiceBusAdministrationClient(string fullyQualifiedNamespace);
    ServiceBusClient GetServiceBusClient(string fullyQualifiedNamespace);
}

public class ServiceBusRepository : IServiceBusRepository
{
    public ServiceBusAdministrationClient GetServiceBusAdministrationClient(string fullyQualifiedNamespace)
    {
        return new ServiceBusAdministrationClient(fullyQualifiedNamespace, new DefaultAzureCredential());
    }

    public ServiceBusClient GetServiceBusClient(string fullyQualifiedNamespace)
    {
        return new ServiceBusClient(fullyQualifiedNamespace, new DefaultAzureCredential());
    }
}
