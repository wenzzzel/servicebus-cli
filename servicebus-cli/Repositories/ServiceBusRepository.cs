using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace servicebus_cli.Repositories;

public interface IServiceBusRepostitory
{
    Task ResendDeadletterMessage(string fullyQualifiedNamespace, string entityPath, string useSession);
}

public class ServiceBusRepository : IServiceBusRepostitory
{
    public async Task ResendDeadletterMessage(string fullyQualifiedNamespace, string entityPath, string useSession)
    {
        var serviceBusClient = new ServiceBusClient(fullyQualifiedNamespace, new DefaultAzureCredential());

        var sender = serviceBusClient.CreateSender(entityPath);
        var receiverOptions = new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter, ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete };
        var receiver = serviceBusClient.CreateReceiver(entityPath, receiverOptions);

        ServiceBusAdministrationClient adminClient = new ServiceBusAdministrationClient(fullyQualifiedNamespace, new DefaultAzureCredential());
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
    }
}
