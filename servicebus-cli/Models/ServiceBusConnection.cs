using Azure.Messaging.ServiceBus;

namespace servicebus_cli.Models;

public class ServiceBusConnection
{
    public ServiceBusReceiver DeadletterReceiver { get; }
    public ServiceBusReceiver MessageReceiver { get; }
    public ServiceBusSender Sender { get; }

    public ServiceBusConnection(ServiceBusReceiver deadletterReceiver, ServiceBusReceiver messageReceiver, ServiceBusSender sender)
    {
        DeadletterReceiver = deadletterReceiver;
        MessageReceiver = messageReceiver;
        Sender = sender;
    }
}

