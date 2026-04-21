using Azure.Messaging.ServiceBus;
using servicebus_cli.Services;
using System.Text.Json;

namespace servicebus_cli.Subjects.Queue.Actions;

public interface IQueueActions
{
    Task List(List<string> args);
    Task Peek(List<string> args);
    Task Purge(List<string> args);
}

public class QueueActions(
    IServiceBusService _serviceBusService,
    IFileService _fileService,
    IUserSettingsService _userSettingsService,
    IConsoleService _consoleService) : IQueueActions
{

    public async Task List(List<string> args)
    {
        var fullyQualifiedNamespace = "";
        var filter = "";
        var settingsFileContent = _fileService.GetConfigFileContent();
        var savedNamespaces = _userSettingsService.Deserialize(settingsFileContent);

        switch (args.Count)
        {
            case 1:
                fullyQualifiedNamespace = args[0];
                break;
            case 2:
                fullyQualifiedNamespace = args[0];
                filter = args[1];
                break;
            default:
                if (!savedNamespaces.FullyQualifiedNamespaces.Any())
                {
                    fullyQualifiedNamespace = await _consoleService.PromptFreeText("Enter the [green]fully qualified namespace[/]:");
                }
                else
                {
                    fullyQualifiedNamespace = await _consoleService.PromptSelection(
                        "Select a fully qualified namespace:",
                        savedNamespaces.FullyQualifiedNamespaces
                    );
                }
                
                _consoleService.WriteMarkup($"[grey]Selected fully qualified namespace: {fullyQualifiedNamespace}[/]");

                filter = await _consoleService.PromptFreeText("Enter a [green]filter[/] (optional):", allowEmpty: true);

                break;
        }

        var queueInfoWorkload = async () =>
        {
            return await _serviceBusService.GetInformationAboutAllQueues(fullyQualifiedNamespace, filter).ConfigureAwait(false);
        };    

        var queuesWithInformation = await _consoleService.ProcessWorkloadWithSpinner(
            $"Listing queues on {fullyQualifiedNamespace}...", 
            queueInfoWorkload);

        var headers = new List<string> { 
            "📮 [bold]Queue Name[/]", 
            "[green]Active[/]", 
            "[red]Dead Letter[/]", 
            "[blue]Scheduled[/]" 
        };

        var rows = new List<List<string>>();

        foreach (var queueInfo in queuesWithInformation)
        {
            var activeMessageCount = queueInfo.QueueRuntimeProperties.ActiveMessageCount;
            var deadLetterMessageCount = queueInfo.QueueRuntimeProperties.DeadLetterMessageCount;
            var scheduledMessageCount = queueInfo.QueueRuntimeProperties.ScheduledMessageCount;

            rows.Add(new List<string> {
                queueInfo.QueueProperties.Name,
                $"[green]{activeMessageCount}[/]",
                $"[red]{deadLetterMessageCount}[/]",
                $"[blue]{scheduledMessageCount}[/]"
            });
        }

        _consoleService.WriteTable(headers, rows);
    }

    public async Task Peek(List<string> args)
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

                var peekQueuesWorkload = async () =>
                {
                    return await _serviceBusService.GetInformationAboutAllQueues(fullyQualifiedNamespace).ConfigureAwait(false);
                };

                var queues = await _consoleService.ProcessWorkloadWithSpinner(
                    $"Fetching queues on {fullyQualifiedNamespace}...",
                    peekQueuesWorkload);

                var selectedQueue = await _consoleService.PromptSelection(
                    "Select a [green]queue[/]:",
                    queues.Select(q => $"{q.QueueProperties.Name} ([green]{q.QueueRuntimeProperties.ActiveMessageCount}[/], [red]{q.QueueRuntimeProperties.DeadLetterMessageCount}[/], [blue]{q.QueueRuntimeProperties.ScheduledMessageCount}[/])").ToList(),
                    enableSearch: true);

                entityPath = selectedQueue.Split(' ')[0];

                _consoleService.WriteMarkup($"[grey]Selected queue: {entityPath}[/]");

                break;
        }

        var requiresSessions = await _serviceBusService.QueueRequiresSessions(fullyQualifiedNamespace, entityPath);
        if (requiresSessions)
        {
            _consoleService.WriteError("Peeking messages on queues with sessions enabled is not currently supported.");
            return;
        }

        var peekWorkload = async () =>
        {
            return await _serviceBusService.PeekMessages(fullyQualifiedNamespace, entityPath).ConfigureAwait(false);
        };

        var messages = await _consoleService.ProcessWorkloadWithSpinner(
            $"Peeking messages on {entityPath}...",
            peekWorkload);

        if (messages.Count == 0)
        {
            _consoleService.WriteError($"No messages found in queue {entityPath}");
            return;
        }

        var jsonMessages = messages.Select(m => new
        {
            m.MessageId,
            Body = m.Body?.ToString(),
            m.Subject,
            m.ContentType,
            m.CorrelationId,
            m.EnqueuedTime,
            m.ExpiresAt,
            m.SequenceNumber,
            m.DeliveryCount,
            ApplicationProperties = m.ApplicationProperties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        });

        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(jsonMessages, jsonOptions);

        _consoleService.WriteJson(json);
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

                var purgeQueuesWorkload = async () =>
                {
                    return await _serviceBusService.GetInformationAboutAllQueues(fullyQualifiedNamespace).ConfigureAwait(false);
                };

                var queues = await _consoleService.ProcessWorkloadWithSpinner(
                    $"Fetching queues on {fullyQualifiedNamespace}...",
                    purgeQueuesWorkload);

                var selectedQueue = await _consoleService.PromptSelection(
                    "Select a [green]queue[/]:",
                    queues.Select(q => $"{q.QueueProperties.Name} ([green]{q.QueueRuntimeProperties.ActiveMessageCount}[/], [red]{q.QueueRuntimeProperties.DeadLetterMessageCount}[/], [blue]{q.QueueRuntimeProperties.ScheduledMessageCount}[/])").ToList(),
                    enableSearch: true);

                entityPath = selectedQueue.Split(' ')[0];

                _consoleService.WriteMarkup($"[grey]Selected queue: {entityPath}[/]");

                break;
        }

        var confirmed = await _consoleService.ConfirmWarning("This action will purge all messages in the queue. Do you want to continue?");

        if (!confirmed)
        {
            _consoleService.WriteMarkup("[red]Operation cancelled.[/]");
            return;
        }

        var requiresSessions = await _serviceBusService.QueueRequiresSessions(fullyQualifiedNamespace, entityPath);
        if (requiresSessions)
        {
            _consoleService.WriteWarning("This queue has sessions enabled. Purging may be significantly slower due to session-based message retrieval.");
        }

        var activeMessageCount = await _serviceBusService.GetActiveMessageCount(fullyQualifiedNamespace, entityPath);
        if (activeMessageCount is null or 0)
        {
            _consoleService.WriteError($"No messages found in queue {entityPath}");
            return;
        }

        var deleteMessagesWorkload = await _serviceBusService.CreateQueuePurgeWorkload(fullyQualifiedNamespace, entityPath);

        await _consoleService.ProcessWorkloadWithStatusUpdates<ServiceBusReceivedMessage, IReadOnlyList<ServiceBusReceivedMessage>>(
            "Deleting",
            "Deleted",
            "This is usually a sign that there are new messages arriving while purging. The purging has stopped after the original count to avoid causing an infinite loop.",
            activeMessageCount.Value,
            deleteMessagesWorkload);
    }
}
