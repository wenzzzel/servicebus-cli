using Azure.Identity;
using Azure.Messaging.ServiceBus;

namespace servicebus_cli.Subjects;

public interface IDeadletter
{
    Task Run(string[] args);
}

public class Deadletter : IDeadletter
{
    private readonly IHelp helpService;

    public Deadletter(IHelp helpService)
    {
        this.helpService = helpService;
    }

    public async Task Run(string[] args)
    {
        Console.WriteLine(">deadletter");
        if (args.Length is not 2 or 3)
        {
            helpService.Run();
            return;
        }

        switch (args[0])
        {
            case "resend":
                await Resend(args.Skip(1).ToList());
                break;
            default:
                helpService.Run();
                break;
        }
    }

    private async Task Resend(List<string> args)
    {
        var fullyQualifiedNamespace = "";
        var entityPath = "";
        var useSession = "";

        if (args.Count == 2)
        {
            fullyQualifiedNamespace = args[0];
            entityPath = args[1];
        }
        else if (args.Count == 3) 
        {
            fullyQualifiedNamespace = args[0];
            entityPath = args[1];
            useSession = args[2];
        }
        else
        {
            helpService.Run();
            return;
        }

        if (useSession != "N" && useSession != "Y")
            useSession = "N";

        Console.WriteLine($">resend fullyQualifiedNamespace: {fullyQualifiedNamespace}, entityPath: {entityPath}, useSessions: {useSession}");

        var serviceBusClient = new ServiceBusClient(fullyQualifiedNamespace, new DefaultAzureCredential());

        var sender = serviceBusClient.CreateSender(entityPath);
        var receiverOptions = new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter, ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete };
        var receiver = serviceBusClient.CreateReceiver(entityPath, receiverOptions);

        IReadOnlyList<ServiceBusReceivedMessage> messages;
        do
        {
            //IF YOU NEED TO BREAK THE DO-WHILE LOOP,
            //MAKE SURE YOU'RE NOT DOING THAT INSIDE THE WHILE LOOP
            //BREAKING AFTER MESSAGES RECEIVED (Inside the loop) WOULD LOOSE THAT DATA
            messages = await receiver.ReceiveMessagesAsync(1000, TimeSpan.FromSeconds(30));
            Console.WriteLine($"Received {messages.Count}");
            var tasks = new List<Task>();
            foreach (var message in messages)
            {
                var sendMessage = new ServiceBusMessage(message);

                if (useSession == "Y")
                    sendMessage.SessionId = message.SessionId;

                tasks.Add(sender.SendMessageAsync(sendMessage));
            }
            await Task.WhenAll(tasks);
            Console.WriteLine($"Sent {messages.Count}");
        } while (messages.Count > 0);
    }
}
