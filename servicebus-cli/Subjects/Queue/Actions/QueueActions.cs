using Azure.Messaging.ServiceBus;
using servicebus_cli.Services;
using System.Text.Json;

namespace servicebus_cli.Subjects.Queue.Actions;

public interface IQueueActions
{
    Task List(List<string> args);
    Task Peek(List<string> args);
    Task Purge(List<string> args);
    Task EditMetadata(List<string> args);
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
        var userSettings = _userSettingsService.Deserialize(settingsFileContent);

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
                if (!userSettings.FullyQualifiedNamespaces.Any())
                {
                    fullyQualifiedNamespace = await _consoleService.PromptFreeText("Enter the [green]fully qualified namespace[/]:");
                }
                else
                {
                    fullyQualifiedNamespace = await _consoleService.PromptSelection(
                        "Select a fully qualified namespace:",
                        userSettings.FullyQualifiedNamespaces
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

        var additionalColumns = userSettings.AdditionalColumns ?? new Dictionary<string, string>();

        var headers = new List<string> { 
            "📮 [bold]Queue Name[/]", 
            "[green]Active[/]", 
            "[red]Dead Letter[/]", 
            "[blue]Scheduled[/]",
            "Sessions"
        };

        foreach (var column in additionalColumns)
        {
            headers.Add(column.Key);
        }

        var rows = new List<List<string>>();

        foreach (var queueInfo in queuesWithInformation)
        {
            var activeMessageCount = queueInfo.QueueRuntimeProperties.ActiveMessageCount;
            var deadLetterMessageCount = queueInfo.QueueRuntimeProperties.DeadLetterMessageCount;
            var scheduledMessageCount = queueInfo.QueueRuntimeProperties.ScheduledMessageCount;
            var requiresSessions = queueInfo.QueueProperties.RequiresSession;

            var row = new List<string> {
                        queueInfo.QueueProperties.Name,
                        $"[green]{activeMessageCount}[/]",
                        $"[red]{deadLetterMessageCount}[/]",
                        $"[blue]{scheduledMessageCount}[/]",
                        requiresSessions ? "[yellow]Yes[/]" : "No"
                    };

            foreach (var column in additionalColumns)
            {
                var value = GetQueueMetadataStringValue(queueInfo.QueueProperties.UserMetadata, column.Value);
                row.Add(value ?? "");
            }

            rows.Add(row);
        }

        _consoleService.WriteTable(headers, rows);
    }

    public async Task Peek(List<string> args)
    {
        var fullyQualifiedNamespace = "";
        var entityPath = "";
        long messagesCountToPeek = 0;
        var settingsFileContent = _fileService.GetConfigFileContent();
        var savedNamespaces = _userSettingsService.Deserialize(settingsFileContent);

        switch (args.Count)
        {
            case 2:
                fullyQualifiedNamespace = args[0];
                entityPath = args[1];
                break;
            case 3:
                fullyQualifiedNamespace = args[0];
                entityPath = args[1];
                messagesCountToPeek = long.Parse(args[2]);
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

                var activeMessageCount = await _serviceBusService.GetActiveMessageCount(fullyQualifiedNamespace, entityPath);
                if (activeMessageCount is null or 0)
                {
                    _consoleService.WriteError($"No active messages found in queue {entityPath}");
                    return;
                }

                messagesCountToPeek = await _consoleService.PromptForLong(
                    $"Enter the [green]number of active messages[/] to peek (max: {queues.First(q => q.QueueProperties.Name == entityPath).QueueRuntimeProperties.ActiveMessageCount}):",
                    minValue: 1,
                    maxValue: queues.First(q => q.QueueProperties.Name == entityPath).QueueRuntimeProperties.ActiveMessageCount);

                if (messagesCountToPeek == 0)
                    messagesCountToPeek = activeMessageCount.Value;

                _consoleService.WriteMarkup($"[grey]Selected queue: {entityPath}[/]");

                break;
        }

        var requiresSessions = await _serviceBusService.QueueRequiresSessions(fullyQualifiedNamespace, entityPath);
        if (requiresSessions)
        {
            _consoleService.WriteError("Peeking messages on queues with sessions enabled is not currently supported.");
            return;
        }

        if (messagesCountToPeek > int.MaxValue)
            messagesCountToPeek = int.MaxValue;

        var peekWorkload = async () =>
        {
            return await _serviceBusService.PeekMessages(fullyQualifiedNamespace, entityPath, (int)messagesCountToPeek).ConfigureAwait(false);
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

    public async Task EditMetadata(List<string> args)
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

                var editMetadataQueuesWorkload = async () =>
                {
                    return await _serviceBusService.GetInformationAboutAllQueues(fullyQualifiedNamespace).ConfigureAwait(false);
                };

                var queues = await _consoleService.ProcessWorkloadWithSpinner(
                    $"Fetching queues on {fullyQualifiedNamespace}...",
                    editMetadataQueuesWorkload);

                var selectedQueue = await _consoleService.PromptSelection(
                    "Select a [green]queue[/]:",
                    queues.Select(q => $"{q.QueueProperties.Name} ([green]{q.QueueRuntimeProperties.ActiveMessageCount}[/], [red]{q.QueueRuntimeProperties.DeadLetterMessageCount}[/], [blue]{q.QueueRuntimeProperties.ScheduledMessageCount}[/])").ToList(),
                    enableSearch: true);

                entityPath = selectedQueue.Split(' ')[0];

                _consoleService.WriteMarkup($"[grey]Selected queue: {entityPath}[/]");

                break;
        }

        var currentMetadata = await _serviceBusService.GetQueueUserMetadata(fullyQualifiedNamespace, entityPath);

        if (string.IsNullOrWhiteSpace(currentMetadata))
            _consoleService.WriteMarkup("[grey]No metadata currently set on this queue.[/]");
        else
        {
            _consoleService.WriteMarkup("[grey]Current metadata:[/]");
            _consoleService.WriteJson(currentMetadata);
            _consoleService.WriteMarkup("");
        }

        var editedMetadata = await _consoleService.OpenInEditor(currentMetadata ?? "");

        if (editedMetadata is null)
            return;

        var newMetadata = string.IsNullOrWhiteSpace(editedMetadata) ? null : editedMetadata;

        if (newMetadata == currentMetadata)
        {
            _consoleService.WriteMarkup("[grey]No changes detected. Metadata was not updated.[/]");
            return;
        }

        await _serviceBusService.UpdateQueueUserMetadata(fullyQualifiedNamespace, entityPath, newMetadata);
        _consoleService.WriteSuccess($"Metadata updated for queue {entityPath}.");
    }

    private static string? GetQueueMetadataStringValue(string? userMetadata, string propertyPath)
    {
        if (string.IsNullOrWhiteSpace(userMetadata))
            return "[grey]-[/]";

        var properties = propertyPath.Split('.');

        try
        {
            using var doc = JsonDocument.Parse(userMetadata);
            JsonElement currentElement = doc.RootElement;
            foreach (var property in properties)
            {
                if (currentElement.ValueKind == JsonValueKind.Object &&
                    currentElement.TryGetProperty(property, out var nextElement))
                {
                    currentElement = nextElement;
                }
                else
                {
                    return "[grey]-[/]";
                }
            }

            var value = currentElement.ValueKind switch
            {
                JsonValueKind.String => currentElement.GetString(),
                JsonValueKind.Null => null,
                _ => currentElement.GetRawText()
            };



            return PrettifyColumnValues(value);
        }
        catch (JsonException)
        {
        }

        return "[grey]-[/]";
    }

    private static string? PrettifyColumnValues(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "[grey]-[/]";

        if (value.Equals("true", StringComparison.OrdinalIgnoreCase))
            return "[green]Yes[/]";
        if (value.Equals("false", StringComparison.OrdinalIgnoreCase))
            return "[red]No[/]";

        return value;
    }

}
