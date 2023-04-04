using Azure.Identity;
using Azure.Messaging.ServiceBus;

var FullyQualifiedNamespace = "<add your namespace>";
var EntityPath = "<add your entity path>";

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
        var sendMessage = new ServiceBusMessage(message)
        {
            SessionId = message.SessionId, //Remove this if queue is not using sessions
        };
        tasks.Add(sender.SendMessageAsync(sendMessage));
    }
    await Task.WhenAll(tasks);
    Console.WriteLine($"Sent {messages.Count}");
} while (messages.Count > 0);