using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace servicebus_cli.Models;

public class ServiceBusConnection
{
    public ServiceBusReceiver DeadletterReceiver { get; }
    public ServiceBusReceiver MessageReceiver { get; }
    public ServiceBusSender Sender { get; }
    public QueueRuntimeProperties QueueRuntimeProperties { get; }
    public QueueProperties QueueProperties { get; }

    public ServiceBusConnection(
        ServiceBusReceiver deadletterReceiver,
        ServiceBusReceiver messageReceiver,
        ServiceBusSender sender,
        QueueRuntimeProperties queueRuntimeProperties,
        QueueProperties queueProperties)
    {
        DeadletterReceiver = deadletterReceiver;
        MessageReceiver = messageReceiver;
        Sender = sender;
        QueueRuntimeProperties = queueRuntimeProperties;
        QueueProperties = queueProperties;
    }
}

