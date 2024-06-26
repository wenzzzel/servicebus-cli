using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using servicebus_cli.Repositories;
using System.Text;
using System.Text.RegularExpressions;

namespace servicebus_cli.Services;

public interface IServiceBusService
{
    Task ResendDeadletterMessage(string fullyQualifiedNamespace, string entityPath, string useSession);
    Task PurgeDeadletterQueue(string fullyQualifiedNamespace, string entityPath);
    Task ListQueues(string fullyQualifiedNamespace, string filter = "");
    Task ShowQueue(string fullyQualifiedNamespace, string queueName);
}

public class ServiceBusService : IServiceBusService
{
    private IServiceBusRepository _serviceBusRepository;


    public ServiceBusService(IServiceBusRepository serviceBusRepostitory)
    {
        _serviceBusRepository = serviceBusRepostitory;
    }

    public async Task ResendDeadletterMessage(string fullyQualifiedNamespace, string entityPath, string useSession)
    {
        var serviceBusClient = _serviceBusRepository.GetServiceBusClient(fullyQualifiedNamespace);

        var sender = serviceBusClient.CreateSender(entityPath);
        var receiverOptions = new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter, ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete };
        var receiver = serviceBusClient.CreateReceiver(entityPath, receiverOptions);

        var adminClient = _serviceBusRepository.GetServiceBusAdministrationClient(fullyQualifiedNamespace);
        QueueRuntimeProperties properties = await adminClient.GetQueueRuntimePropertiesAsync(entityPath);
        var dlTotalMessageCount = properties.DeadLetterMessageCount;

        Console.WriteLine("WARNING: Stopping the application before it's finished may result in data loss!");
        var resentDlCount = 0;
        IReadOnlyList<ServiceBusReceivedMessage> messages;
        do
        {
            messages = await receiver.ReceiveMessagesAsync(1000, TimeSpan.FromSeconds(30));
            var tasks = new List<Task>();
            
            foreach (var message in messages)
            {
                var sendMessage = new ServiceBusMessage(message);

                if (useSession == "Y")
                    sendMessage.SessionId = message.SessionId;

                tasks.Add(sender.SendMessageAsync(sendMessage));
            }
            await Task.WhenAll(tasks);
            resentDlCount += messages.Count;
            Console.WriteLine($"Sent {resentDlCount} / {dlTotalMessageCount}");
        } while (messages.Count > 0 && resentDlCount < dlTotalMessageCount);

        if (resentDlCount > dlTotalMessageCount)
        {
            Console.WriteLine(@$"INFO: The count of resent messages ({resentDlCount}) was greater then the initial 
                deadletter count ({dlTotalMessageCount}). This may happen due to that deadletters are re-sent and end up on 
                the deadletter queue again, before the resend job was able to finish. It is an indicator that there are bad 
                messages on your deadletter queue that should be handled and/or removed instead of resent.");
        }
    }

    public async Task PurgeDeadletterQueue(string fullyQualifiedNamespace, string queueName)
    {
        var serviceBusClient = _serviceBusRepository.GetServiceBusClient(fullyQualifiedNamespace);
        
        var adminClient = _serviceBusRepository.GetServiceBusAdministrationClient(fullyQualifiedNamespace);
        var properties = await adminClient.GetQueueRuntimePropertiesAsync(queueName);

        if(properties is null)
        {
            Console.Write($" ❌ No queue found with name {queueName}");
            return;
        }

        var deadLetterMessageCount = properties.Value.DeadLetterMessageCount;

        if (deadLetterMessageCount == 0)
        {
            Console.Write($" ❌ No deadletter messages found in queue {queueName}");
            return;
        }

        var receiverOptions = new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter, ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete };
        var receiver = serviceBusClient.CreateReceiver(queueName, receiverOptions);

        var deleteDlCount = 0;
        IReadOnlyList<ServiceBusReceivedMessage> messages;
        do
        {
            messages = await receiver.ReceiveMessagesAsync(1000, TimeSpan.FromSeconds(30));

            deleteDlCount += messages.Count;
            Console.WriteLine($"Deleted {deleteDlCount} / {deadLetterMessageCount}");
        } while (messages.Count > 0 && deleteDlCount < deadLetterMessageCount);

        if (deleteDlCount > deadLetterMessageCount)
        {
            Console.WriteLine(@$"INFO: The count of deleted messages ({deleteDlCount}) was greater then the initial 
                deadletter count ({deadLetterMessageCount}). This may happen due to that deadletters are appearing on 
                the deadletter queue while deleting. It might be good idea to investigate why this is happening.");
        }
    }

    public async Task ListQueues(string fullyQualifiedNamespace, string filter = "")
    {
        var adminClient = _serviceBusRepository.GetServiceBusAdministrationClient(fullyQualifiedNamespace);
        var queues = new List<QueueProperties>();
        await foreach (var queue in adminClient.GetQueuesAsync())
        {
            queues.Add(queue);
        }

        foreach (var queue in queues)
        {
            var regex = $".*{filter}.*";
            Match match = Regex.Match(queue.Name, regex, RegexOptions.IgnoreCase);

            if (!match.Success)
                continue;

            var properties = await adminClient.GetQueueRuntimePropertiesAsync(queue.Name);

            var activeMessageCount = properties.Value.ActiveMessageCount;
            var deadLetterMessageCount = properties.Value.DeadLetterMessageCount;
            var scheduledMessageCount = properties.Value.ScheduledMessageCount;

            Console.OutputEncoding = Encoding.UTF8;
            Console.Write($" 📮 {queue.Name} (");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"{activeMessageCount}");
            Console.ResetColor();
            Console.Write($", ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"{deadLetterMessageCount}");
            Console.ResetColor();
            Console.Write($", ");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write($"{scheduledMessageCount}");
            Console.ResetColor();
            Console.Write($")");
            Console.WriteLine();
        }
    }

    public async Task ShowQueue(string fullyQualifiedNamespace, string queueName)
    {
        var adminClient = _serviceBusRepository.GetServiceBusAdministrationClient(fullyQualifiedNamespace);
        var properties = await adminClient.GetQueueRuntimePropertiesAsync(queueName);

        if(properties is null)
        {
            Console.Write($" ❌ No queue found with name {queueName}");
            return;
        }

        var activeMessageCount = properties.Value.ActiveMessageCount;
        var deadLetterMessageCount = properties.Value.DeadLetterMessageCount;
        var scheduledMessageCount = properties.Value.ScheduledMessageCount;

        Console.OutputEncoding = Encoding.UTF8;
        Console.Write($" 📮 {queueName} (");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write($"{activeMessageCount}");
        Console.ResetColor();
        Console.Write($", ");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write($"{deadLetterMessageCount}");
        Console.ResetColor();
        Console.Write($", ");
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.Write($"{scheduledMessageCount}");
        Console.ResetColor();
        Console.Write($")");
        Console.WriteLine();
    }
}
