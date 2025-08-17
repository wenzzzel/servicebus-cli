using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using servicebus_cli.Models;
using servicebus_cli.Repositories;
using System.Text.RegularExpressions;

namespace servicebus_cli.Services;

public interface IServiceBusService
{
    Task<List<QueueInformation>> GetInformationAboutAllQueues(string fullyQualifiedNamespace, string filter = "");
    Task<long?> GetDeadLetterCount(string fullyQualifiedNamespace, string entityPath);
    Task<ServiceBusConnection> ConnectToQueue(string fullyQualifiedNamespace, string entityPath);
}

public class ServiceBusService(IServiceBusRepository serviceBusRepostitory) : IServiceBusService
{
    private readonly IServiceBusRepository _serviceBusRepository = serviceBusRepostitory;

    public async Task<List<QueueInformation>> GetInformationAboutAllQueues(string fullyQualifiedNamespace, string filter = "")
    {
        var adminClient = _serviceBusRepository.GetServiceBusAdministrationClient(fullyQualifiedNamespace);
        var queues = new List<QueueProperties>();
        
        // Collect all queue properties first
        await foreach (var queue in adminClient.GetQueuesAsync().ConfigureAwait(false))
        {
            var regex = $".*{filter}.*";
            Match match = Regex.Match(queue.Name, regex, RegexOptions.IgnoreCase);

            if (!match.Success)
                continue;

            queues.Add(queue);
        }

        // Now get runtime properties for each queue
        var retVal = new List<QueueInformation>();
        foreach (var queue in queues)
        {
            var properties = await adminClient.GetQueueRuntimePropertiesAsync(queue.Name).ConfigureAwait(false);
            var model = new QueueInformation(queue, properties.Value);
            retVal.Add(model);
        }

        // Ensure all async operations are completed
        await Task.Yield();
        
        return retVal;
    }

    public async Task<long?> GetDeadLetterCount(string fullyQualifiedNamespace, string entityPath)
    {
        var adminClient = _serviceBusRepository.GetServiceBusAdministrationClient(fullyQualifiedNamespace);
        var properties = await adminClient.GetQueueRuntimePropertiesAsync(entityPath);
        return properties?.Value?.DeadLetterMessageCount;
    }

    public async Task<ServiceBusConnection> ConnectToQueue(string fullyQualifiedNamespace, string entityPath)
    {
        // Get info about queue (might be nice to have)
        var adminClient = _serviceBusRepository.GetServiceBusAdministrationClient(fullyQualifiedNamespace);
        var runtimeProperties = await adminClient.GetQueueRuntimePropertiesAsync(entityPath);
        var queueProperties = await adminClient.GetQueueAsync(entityPath);

        // Connect to the actual receivers and senders
        var deadletterReceiverOptions = new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter, ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete };
        var messageReceiverOptions = new ServiceBusReceiverOptions { SubQueue = SubQueue.None, ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete };
        var serviceBusClient = _serviceBusRepository.GetServiceBusClient(fullyQualifiedNamespace);
        var deadletterReceiver = serviceBusClient.CreateReceiver(entityPath, deadletterReceiverOptions);
        var messageReceiver = serviceBusClient.CreateReceiver(entityPath, messageReceiverOptions);
        var sender = serviceBusClient.CreateSender(entityPath);

        var connection = new ServiceBusConnection(deadletterReceiver, messageReceiver, sender, runtimeProperties, queueProperties);

        return connection;
    }
}
