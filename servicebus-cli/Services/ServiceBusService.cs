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
    Task<long?> GetActiveMessageCount(string fullyQualifiedNamespace, string entityPath);
    Task<bool> QueueRequiresSessions(string fullyQualifiedNamespace, string entityPath);
    Task<ServiceBusConnection> ConnectToQueue(string fullyQualifiedNamespace, string entityPath);
    Task<IReadOnlyList<ServiceBusReceivedMessage>> PeekDeadLetterMessages(string fullyQualifiedNamespace, string entityPath, int maxMessages = 1000);
    Task<IReadOnlyList<ServiceBusReceivedMessage>> PeekMessages(string fullyQualifiedNamespace, string entityPath, int maxMessages = 1000);
    Task<Func<Task<IReadOnlyList<ServiceBusReceivedMessage>>>> CreateQueuePurgeWorkload(string fullyQualifiedNamespace, string entityPath);
}

public class ServiceBusService(IServiceBusRepository _serviceBusRepository) : IServiceBusService
{
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

    public async Task<long?> GetActiveMessageCount(string fullyQualifiedNamespace, string entityPath)
    {
        var adminClient = _serviceBusRepository.GetServiceBusAdministrationClient(fullyQualifiedNamespace);
        var properties = await adminClient.GetQueueRuntimePropertiesAsync(entityPath);
        return properties?.Value?.ActiveMessageCount;
    }

    public async Task<bool> QueueRequiresSessions(string fullyQualifiedNamespace, string entityPath)
    {
        var adminClient = _serviceBusRepository.GetServiceBusAdministrationClient(fullyQualifiedNamespace);
        var queueProperties = await adminClient.GetQueueAsync(entityPath);
        return queueProperties.Value.RequiresSession;
    }

    public async Task<IReadOnlyList<ServiceBusReceivedMessage>> PeekDeadLetterMessages(string fullyQualifiedNamespace, string entityPath, int maxMessages = 1000)
    {
        var serviceBusClient = _serviceBusRepository.GetServiceBusClient(fullyQualifiedNamespace);
        var receiverOptions = new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter };
        var receiver = serviceBusClient.CreateReceiver(entityPath, receiverOptions);

        var allMessages = new List<ServiceBusReceivedMessage>();
        while (allMessages.Count < maxMessages)
        {
            var batch = await receiver.PeekMessagesAsync(maxMessages - allMessages.Count);
            if (batch.Count == 0)
                break;
            allMessages.AddRange(batch);
        }

        return allMessages;
    }

    public async Task<IReadOnlyList<ServiceBusReceivedMessage>> PeekMessages(string fullyQualifiedNamespace, string entityPath, int maxMessages = 1000)
    {
        var serviceBusClient = _serviceBusRepository.GetServiceBusClient(fullyQualifiedNamespace);

        var allMessages = new List<ServiceBusReceivedMessage>();

        var receiver = serviceBusClient.CreateReceiver(entityPath);
        while (allMessages.Count < maxMessages)
        {
            var batch = await receiver.PeekMessagesAsync(maxMessages - allMessages.Count);
            if (batch.Count == 0)
                break;
            allMessages.AddRange(batch);
        }

        return allMessages;
    }

    public async Task<Func<Task<IReadOnlyList<ServiceBusReceivedMessage>>>> CreateQueuePurgeWorkload(string fullyQualifiedNamespace, string entityPath)
    {
        var adminClient = _serviceBusRepository.GetServiceBusAdministrationClient(fullyQualifiedNamespace);
        var queueProperties = await adminClient.GetQueueAsync(entityPath);
        var serviceBusClient = _serviceBusRepository.GetServiceBusClient(fullyQualifiedNamespace);

        if (queueProperties.Value.RequiresSession)
        {
            var sessionReceiverOptions = new ServiceBusSessionReceiverOptions
            {
                ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete
            };

            return async () =>
            {
                var tasks = Enumerable.Range(0, 10).Select(async _ =>
                {
                    var sessionMessages = new List<ServiceBusReceivedMessage>();
                    try
                    {
                        var sessionReceiver = await serviceBusClient.AcceptNextSessionAsync(entityPath, sessionReceiverOptions);
                        while (true)
                        {
                            var batch = await sessionReceiver.ReceiveMessagesAsync(1000, TimeSpan.FromSeconds(2));
                            if (batch.Count == 0) break;
                            sessionMessages.AddRange(batch);
                        }
                        await sessionReceiver.CloseAsync();
                    }
                    catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.ServiceTimeout)
                    {
                        // No more sessions available
                    }
                    return sessionMessages;
                });

                var results = await Task.WhenAll(tasks);
                return (IReadOnlyList<ServiceBusReceivedMessage>)results.SelectMany(r => r).ToList();
            };
        }

        var receiverOptions = new ServiceBusReceiverOptions
        {
            SubQueue = SubQueue.None,
            ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete
        };
        var receiver = serviceBusClient.CreateReceiver(entityPath, receiverOptions);

        return async () =>
        {
            return await receiver.ReceiveMessagesAsync(1000, TimeSpan.FromSeconds(30));
        };
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
