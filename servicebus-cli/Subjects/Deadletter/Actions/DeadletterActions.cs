using Azure.Messaging.ServiceBus;
using servicebus_cli.Services;
using Spectre.Console;

namespace servicebus_cli.Subjects.Deadletter.Actions;

public interface IDeadletterActions
{
    Task Resend(List<string> args);
    Task Purge(List<string> args);
}

public class DeadletterActions(
    IServiceBusService _serviceBusService,
    IFileService _fileService,
    IUserSettingsService _userSettingsService,
    IConsoleService _consoleService) : IDeadletterActions
{
    public async Task Resend(List<string> args)
    {
        var fullyQualifiedNamespace = "";
        var entityPath = "";
        var settingsFileContent = _fileService.GetConfigFileContent();
        var savedNamespaces = _userSettingsService.Deserialize(settingsFileContent);

        switch (args.Count)
        {
            case 2:
                fullyQualifiedNamespace = args[0];
                entityPath = args[1];
                break;
            default:
                if (!savedNamespaces.FullyQualifiedNamespaces.Any())
                {
                    fullyQualifiedNamespace = await _consoleService.PromptFreeText(
                        "Enter the [green]fully qualified namespace[/]:");
                }
                else
                {
                    fullyQualifiedNamespace = await _consoleService.PromptSelection(
                        "Select a fully qualified namespace:",
                        savedNamespaces.FullyQualifiedNamespaces);
                }

                _consoleService.WriteMarkup($"[grey]Selected fully qualified namespace: {fullyQualifiedNamespace}[/]");

                var queues = await AnsiConsole.Status()
                    .StartAsync($"Fetching queues on {fullyQualifiedNamespace}...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("yellow"));

                        return await _serviceBusService.GetInformationAboutAllQueues(fullyQualifiedNamespace).ConfigureAwait(false);
                    });


                var selectedQueue = await _consoleService.PromptSelection(
                    "Select a [green]queue[/]:",
                    queues.Select(q => $"{q.QueueProperties.Name} ([green]{q.QueueRuntimeProperties.ActiveMessageCount}[/], [red]{q.QueueRuntimeProperties.DeadLetterMessageCount}[/], [blue]{q.QueueRuntimeProperties.ScheduledMessageCount}[/])").ToList(),
                    enableSearch: true);

                entityPath = selectedQueue.Split(' ')[0];

                _consoleService.WriteMarkup($"[grey]Selected queue: {entityPath}[/]");

                break;
        }

        var confirmed = await _consoleService.ConfirmWarning($"This action will resend all deadletter messages. Stopping the application before it's finished may result in data loss! Do you want to continue?");

        if (!confirmed)
        {
            _consoleService.WriteMarkup("[red]Operation cancelled.[/]");
            return;
        }

        var deadletterCount = await _serviceBusService.GetDeadLetterCount(fullyQualifiedNamespace, entityPath);
        if (deadletterCount is null or 0)
        {
            _consoleService.WriteError($"No deadletter messages found in queue {entityPath}");
            return;
        }

        var queue = await _serviceBusService.ConnectToQueue(fullyQualifiedNamespace, entityPath);
        var resentDlCount = 0;
        IReadOnlyList<ServiceBusReceivedMessage> messages;

        //TODO: Try to use ProcessWorkloadWithStatusUpdates here
        await AnsiConsole.Status().StartAsync($"Resending messages... 0 / {deadletterCount}", async ctx =>
        {
            do
            {
                messages = await queue.DeadletterReceiver.ReceiveMessagesAsync(1000, TimeSpan.FromSeconds(30));
                var tasks = new List<Task>();

                foreach (var message in messages)
                {
                    var sendMessage = new ServiceBusMessage(message);

                    if (queue.QueueProperties.RequiresSession) //Only set session id if the queue supports sessions
                        sendMessage.SessionId = message.SessionId;

                    tasks.Add(queue.Sender.SendMessageAsync(sendMessage));
                }
                await Task.WhenAll(tasks);
                resentDlCount += messages.Count;
                ctx.Status($"Resending messages... {resentDlCount} / {deadletterCount}");
            } while (messages.Count > 0 && resentDlCount < deadletterCount);
        });

        if (resentDlCount > deadletterCount)
            _consoleService.WriteWarning($"The count of resent messages ({resentDlCount}) was greater than the initial deadletter count ({deadletterCount}). This may happen due to deadletters being re-sent and ending up on the deadletter queue again before the resend job was able to finish. It is an indicator that there are bad messages on your deadletter queue that should be handled and/or removed instead of resent.");
        else
            _consoleService.WriteSuccess($"Resent {resentDlCount} messages from deadletter queue {entityPath}");
    }

    public async Task Purge(List<string> args)
    {
        var fullyQualifiedNamespace = "";
        var entityPath = "";
        var settingsFileContent = _fileService.GetConfigFileContent();
        var savedNamespaces = _userSettingsService.Deserialize(settingsFileContent);

        switch (args.Count)
        {
            case 2:
                fullyQualifiedNamespace = args[0];
                entityPath = args[1];
                break;
            default:
                if (!savedNamespaces.FullyQualifiedNamespaces.Any())
                {
                    fullyQualifiedNamespace = await _consoleService.PromptFreeText(
                        "Enter the [green]fully qualified namespace[/]:");
                }
                else
                {
                    fullyQualifiedNamespace = await _consoleService.PromptSelection(
                        "Select a fully qualified namespace:",
                        savedNamespaces.FullyQualifiedNamespaces);
                }

                _consoleService.WriteMarkup($"[grey]Selected fully qualified namespace: {fullyQualifiedNamespace}[/]");

                var asyncWorkload = async () =>
                {
                    return await _serviceBusService.GetInformationAboutAllQueues(fullyQualifiedNamespace).ConfigureAwait(false);
                };

                var queues = await _consoleService.ProcessWorkloadWithSpinner($"Fetching queues on {fullyQualifiedNamespace}...", asyncWorkload);

                var selectedQueue = await _consoleService.PromptSelection(
                    "Select a [green]queue[/]:",
                    queues.Select(q => $"{q.QueueProperties.Name} ([green]{q.QueueRuntimeProperties.ActiveMessageCount}[/], [red]{q.QueueRuntimeProperties.DeadLetterMessageCount}[/], [blue]{q.QueueRuntimeProperties.ScheduledMessageCount}[/])").ToList(),
                    enableSearch: true);

                entityPath = selectedQueue.Split(' ')[0];

                _consoleService.WriteMarkup($"[grey]Selected queue: {entityPath}[/]");

                break;
        }
        
        var confirmed = await _consoleService.ConfirmWarning("This action will purge all deadletter messages. Do you want to continue?");

        if (!confirmed)
        {
            _consoleService.WriteMarkup("[red]Operation cancelled.[/]");
            return;
        }

        var deadletterCountTotal = await _serviceBusService.GetDeadLetterCount(fullyQualifiedNamespace, entityPath);
        if (deadletterCountTotal is null or 0)
        {
            _consoleService.WriteError($"No deadletter messages found in queue {entityPath}");
            return;
        }

        var queue = await _serviceBusService.ConnectToQueue(fullyQualifiedNamespace, entityPath);

        var asyncWorkload2 = async () =>
        {
            return await queue.DeadletterReceiver.ReceiveMessagesAsync(1000, TimeSpan.FromSeconds(30)); //Simply receiving messages deletes them as well
        };

        await _consoleService.ProcessWorkloadWithStatusUpdates<ServiceBusReceivedMessage, IReadOnlyList<ServiceBusReceivedMessage>>(
            "Deleting",
            "Deleted",
            "This is usually a sign that there are new deadletter messages arriving while purging. It might be good idea to investigate why this is happening.",
            deadletterCountTotal.Value,
            asyncWorkload2);
    }
}
