using Azure.Identity;
using Azure.Messaging.ServiceBus;

namespace servicebus_cli.Subjects;

internal static class Deadletter
{
    internal static void Run(string[] args)
    {
        Console.WriteLine("It seems that you want to work with deadletters");
        if(args.Length == 0)
            Help.Run();

        switch (args[0])
        {
            case "resend":
                Resend(args[1], args[2]);
                break;
            default:
                Help.Run();
                break;
        }
    }
    
    private async static void Resend(string FullyQualifiedNamespace, string EntityPath)
    {
        Console.WriteLine($"It seems that you want to resend for {EntityPath} on {FullyQualifiedNamespace}");
        //var FullyQualifiedNamespace = "emea-grip-ip-async-sbus-prod.servicebus.windows.net";
        //var EntityPath = "deltavehicle";

        var serviceBusClient = new ServiceBusClient(FullyQualifiedNamespace, new DefaultAzureCredential());

        var sender = serviceBusClient.CreateSender(EntityPath);
        var receiverOptions = new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter, ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete };
        var receiver = serviceBusClient.CreateReceiver(EntityPath, receiverOptions);

        IReadOnlyList<ServiceBusReceivedMessage> messages;
        do
        {
            //IF YOU NEED TO BREAK THE DO-WHILE LOOP,
            //MAKE SURE YOU'RE DOING THAT HERE ON LINE 19 USING A BREAKPOINT AND NOT WHILE THE LOOP IS RUNNING.
            //BREAKING AFTER MESSAGES RECEIVED ON LINE 20 IN THE ITERATION WOULD LOOSE THAT DATA
            messages = await receiver.ReceiveMessagesAsync(1000, TimeSpan.FromSeconds(10));
            Console.WriteLine($"Received {messages.Count}");
            var tasks = new List<Task>();
            foreach (var message in messages)
            {
                var sendMessage = new ServiceBusMessage(message);
                //{
                //    SessionId = message.SessionId, //Remove this if queue is not using sessions
                //};
                tasks.Add(sender.SendMessageAsync(sendMessage));
            }
            await Task.WhenAll(tasks);
            Console.WriteLine($"Sent {messages.Count}");
        } while (messages.Count > 0);
    }
}
