using Azure.Messaging.ServiceBus.Administration;

namespace servicebus_cli.Models;

public class QueueModel
{
    public QueueProperties QueueProperties { get; }
    public QueueRuntimeProperties QueueRuntimeProperties { get; }

    public QueueModel(QueueProperties queueProperties, QueueRuntimeProperties queueRuntimeProperties)
    {
        QueueProperties = queueProperties;
        QueueRuntimeProperties = queueRuntimeProperties;
    }
}

