using Azure.Messaging.ServiceBus;
using servicebus_cli.Services;
using Spectre.Console;

namespace servicebus_cli.Subjects;

public interface IDeadletter
{
    Task Run(string[] args);
}

public class Deadletter : IDeadletter
{
    private readonly IHelp _helpService;
    private readonly IServiceBusService _serviceBusRepostitory;

    public Deadletter(IHelp helpService, IServiceBusService serviceBusRepostitory)
    {
        _helpService = helpService;
        _serviceBusRepostitory = serviceBusRepostitory;
    }

    public async Task Run(string[] args)
    {
        string selectedAction = "";
        if (args.Length < 1)
        {
            selectedAction = await AnsiConsole.PromptAsync(
                new SelectionPrompt<string>()
                    .Title("Action: ")
                    .PageSize(10)
                    .AddChoices(
                        "resend",
                        "purge"
                    )
            );
        }
        else
        {
            selectedAction = args[0];
        }

        AnsiConsole.MarkupLine($"[grey]Selected action: {selectedAction}[/]");

        switch (selectedAction)
        {
            case "resend":
                await Resend(args.Skip(1).ToList());
                break;
            case "purge":
                await Purge(args.Skip(1).ToList());
                break;
            default:
                _helpService.Run();
                break;
        }
    }

    private async Task Resend(List<string> args)
    {
        var fullyQualifiedNamespace = "";
        var entityPath = "";
        bool useSession = false;

        switch (args.Count)
        {
            case 2:
                fullyQualifiedNamespace = args[0];
                entityPath = args[1];
                break;
            case 3:
                fullyQualifiedNamespace = args[0];
                entityPath = args[1];
                useSession = args[2].ToUpper() == "Y";
                break;
            default:
                fullyQualifiedNamespace = await AnsiConsole.PromptAsync(
                    new TextPrompt<string>("Enter the [green]fully qualified namespace[/]:")
                );

                var queues = await _serviceBusRepostitory.GetQueues(fullyQualifiedNamespace);
                var selectedQueue = await AnsiConsole.PromptAsync(
                    new SelectionPrompt<string>()
                        .Title("Select a [green]queue[/]:")
                        .PageSize(30)
                        .EnableSearch()
                        .AddChoices(queues.Select(q => $"{q.QueueProperties.Name} ([green]{q.QueueRuntimeProperties.ActiveMessageCount}[/], [red]{q.QueueRuntimeProperties.DeadLetterMessageCount}[/], [blue]{q.QueueRuntimeProperties.ScheduledMessageCount}[/])").ToList())
                );
                entityPath = selectedQueue.Split(' ')[0];
                useSession = await AnsiConsole.ConfirmAsync("Use session?");
                break;
        }

        var confirmed = await AnsiConsole.ConfirmAsync(@$"[red]WARNING:[/] This action will resend all deadletter messages. Stopping the application before it's finished may result in data loss! Do you want to continue?");

        if (!confirmed)
        {
            AnsiConsole.MarkupLine("[red]Operation cancelled.[/]");
            return;
        }

        var deadletterCount = await _serviceBusRepostitory.GetDeadLetterCount(fullyQualifiedNamespace, entityPath);
        if (deadletterCount is null or 0)
        {
            AnsiConsole.MarkupLine($"[red]ERROR:[/] No deadletter messages found in queue {entityPath}");
            return;
        }

        var queue = _serviceBusRepostitory.ConnectToQueue(fullyQualifiedNamespace, entityPath);
        var resentDlCount = 0;
        IReadOnlyList<ServiceBusReceivedMessage> messages;
        await AnsiConsole.Status().StartAsync($"Resending messages... 0 / {deadletterCount}", async ctx =>
        {
            do
            {
                messages = await queue.DeadletterReceiver.ReceiveMessagesAsync(1000, TimeSpan.FromSeconds(30));
                var tasks = new List<Task>();

                foreach (var message in messages)
                {
                    var sendMessage = new ServiceBusMessage(message);

                    if (useSession)
                        sendMessage.SessionId = message.SessionId;

                    tasks.Add(queue.Sender.SendMessageAsync(sendMessage));
                }
                await Task.WhenAll(tasks);
                resentDlCount += messages.Count;
                ctx.Status($"Resending messages... {resentDlCount} / {deadletterCount}");
            } while (messages.Count > 0 && resentDlCount < deadletterCount);
        });

        if (resentDlCount > deadletterCount)
            AnsiConsole.MarkupLine(@$"[red]WARNING:[/] The count of resent messages ({resentDlCount}) was greater than the 
            initial deadletter count ({deadletterCount}). This may happen due to deadletters being re-sent and 
            ending up on the deadletter queue again before the resend job was able to finish. It is an indicator that 
            there are bad messages on your deadletter queue that should be handled and/or removed instead of resent.");
        else
            AnsiConsole.MarkupLine(@$"[green]Success![/] [grey]Resent {resentDlCount} messages from deadletter queue {entityPath}[/]");
    }

    private async Task Purge(List<string> args)
    {
        var fullyQualifiedNamespace = "";
        var entityPath = "";

        switch (args.Count)
        {
            case 2:
                fullyQualifiedNamespace = args[0];
                entityPath = args[1];
                break;
            default:
                fullyQualifiedNamespace = await AnsiConsole.PromptAsync(
                    new TextPrompt<string>("Enter the [green]fully qualified namespace[/]:")
                );
                var queues = await _serviceBusRepostitory.GetQueues(fullyQualifiedNamespace);
                var selectedQueue = await AnsiConsole.PromptAsync(
                    new SelectionPrompt<string>()
                        .Title("Select a [green]queue[/]:")
                        .PageSize(30)
                        .EnableSearch()
                        .AddChoices(queues.Select(q => $"{q.QueueProperties.Name} ([green]{q.QueueRuntimeProperties.ActiveMessageCount}[/], [red]{q.QueueRuntimeProperties.DeadLetterMessageCount}[/], [blue]{q.QueueRuntimeProperties.ScheduledMessageCount}[/])").ToList())
                );
                entityPath = selectedQueue.Split(' ')[0];
                break;
        }

        var confirmed = await AnsiConsole.ConfirmAsync($"[red]WARNING:[/] This action will purge all deadletter messages. Do you want to continue?");

        if (!confirmed)
        {
            AnsiConsole.MarkupLine("[red]Operation cancelled.[/]");
            return;
        }

        var deadletterCountTotal = await _serviceBusRepostitory.GetDeadLetterCount(fullyQualifiedNamespace, entityPath);
        if (deadletterCountTotal is null or 0)
        {
            AnsiConsole.MarkupLine($"[red]ERROR:[/] No deadletter messages found in queue {entityPath}");
            return;
        }

        var queue = _serviceBusRepostitory.ConnectToQueue(fullyQualifiedNamespace, entityPath);

        var deletedDeadletterCount = 0;
        IReadOnlyList<ServiceBusReceivedMessage> messages;
        await AnsiConsole.Status().StartAsync($"Deleting messages... 0 / {deadletterCountTotal}", async ctx =>
        {
            do
            {
                messages = await queue.DeadletterReceiver.ReceiveMessagesAsync(1000, TimeSpan.FromSeconds(30)); //Simply receiving messages deletes them as well

                deletedDeadletterCount += messages.Count;
                ctx.Status($"Deleting messages... {deletedDeadletterCount} / {deadletterCountTotal}");

            } while (messages.Count > 0 && deletedDeadletterCount < deadletterCountTotal);
        });

        if (deletedDeadletterCount > deadletterCountTotal)
            AnsiConsole.MarkupLine(@$"[red]WARNING:[/] The count of deleted messages ({deletedDeadletterCount}) was greater 
            than the initial deadletter count ({deadletterCountTotal}). This may happen due to that deadletters are 
            appearing on the deadletter queue while deleting. It might be good idea to investigate why this is happening.");
        else
            AnsiConsole.MarkupLine(@$"[green]Success![/] [grey]Deleted {deletedDeadletterCount} messages from deadletter 
            queue {entityPath}[/]");
    }
}
