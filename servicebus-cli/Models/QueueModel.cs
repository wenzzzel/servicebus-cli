using Azure.Messaging.ServiceBus.Administration;

namespace servicebus_cli.Models;

public class QueueInformation
{
    public QueueProperties QueueProperties { get; }
    public QueueRuntimeProperties QueueRuntimeProperties { get; }

    public QueueInformation(QueueProperties queueProperties, QueueRuntimeProperties queueRuntimeProperties)
    {
        QueueProperties = queueProperties;
        QueueRuntimeProperties = queueRuntimeProperties;
    }
}

