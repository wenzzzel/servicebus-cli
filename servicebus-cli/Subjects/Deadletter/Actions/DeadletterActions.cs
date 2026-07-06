using Azure.Messaging.ServiceBus;
using servicebus_cli.Services;
using System.Text.Json;

namespace servicebus_cli.Subjects.Deadletter.Actions;

public interface IDeadletterActions
{
    Task Resend(List<string> args);
    Task Purge(List<string> args);
    Task Peek(List<string> args);
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
        long messagesCountToResend = 0;
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
                messagesCountToResend = long.Parse(args[2]);
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

                var getQueuesWorkload = async () =>
                {
                    return await _serviceBusService.GetInformationAboutAllQueues(fullyQualifiedNamespace).ConfigureAwait(false);
                };

                var queues = await _consoleService.ProcessWorkloadWithSpinner(
                    $"Fetching queues on {fullyQualifiedNamespace}...",
                    getQueuesWorkload);

                var selectedQueue = await _consoleService.PromptSelection(
                    "Select a [green]queue[/]:",
                    queues.Select(q => $"{q.QueueProperties.Name} ([green]{q.QueueRuntimeProperties.ActiveMessageCount}[/], [red]{q.QueueRuntimeProperties.DeadLetterMessageCount}[/], [blue]{q.QueueRuntimeProperties.ScheduledMessageCount}[/])").ToList(),
                    enableSearch: true);

                entityPath = selectedQueue.Split(' ')[0];

                var deadletterCount = await _serviceBusService.GetDeadLetterCount(fullyQualifiedNamespace, entityPath);
                if (deadletterCount is null or 0)
                {
                    _consoleService.WriteError($"No deadletter messages found in queue {entityPath}");
                    return;
                }

                messagesCountToResend = await _consoleService.PromptForLong(
                    $"Enter the [green]number of deadletter messages[/] to resend (max: {queues.First(q => q.QueueProperties.Name == entityPath).QueueRuntimeProperties.DeadLetterMessageCount}):",
                    minValue: 1,
                    maxValue: queues.First(q => q.QueueProperties.Name == entityPath).QueueRuntimeProperties.DeadLetterMessageCount);

                if (messagesCountToResend == 0)
                    messagesCountToResend = deadletterCount.Value;

                _consoleService.WriteMarkup($"[grey]Selected queue: {entityPath}[/]");

                break;
        }

        var confirmed = await _consoleService.ConfirmWarning($"This action will resend {messagesCountToResend} deadletter messages. Stopping the application before it's finished may result in data loss! Do you want to continue?");

        if (!confirmed)
        {
            _consoleService.WriteMarkup("[red]Operation cancelled.[/]");
            return;
        }



        var queue = await _serviceBusService.ConnectToQueue(fullyQualifiedNamespace, entityPath);

        var resendMessagesWorkload = async () =>
        {
            var messageCount = 0;
            var maxMessageBatchSize = messagesCountToResend > 1000 ? 1000 : (int)messagesCountToResend;
            var messages = await queue.DeadletterReceiver.ReceiveMessagesAsync(maxMessageBatchSize, TimeSpan.FromSeconds(30));

            var batch = await queue.Sender.CreateMessageBatchAsync();

            foreach (var message in messages)
            {
                messageCount++;

                var sendMessage = new ServiceBusMessage(message);

                if (queue.QueueProperties.RequiresSession) //Only set session id if the queue supports sessions
                    sendMessage.SessionId = message.SessionId;

                if (!batch.TryAddMessage(sendMessage))
                {
                    await queue.Sender.SendMessagesAsync(batch);
                    batch = await queue.Sender.CreateMessageBatchAsync();
                    batch.TryAddMessage(sendMessage);
                }
            }

            if (batch.Count > 0 && messageCount < messagesCountToResend)
                await queue.Sender.SendMessagesAsync(batch);

            return messages;
        };

        await _consoleService.ProcessWorkloadWithStatusUpdates<ServiceBusReceivedMessage, IReadOnlyList<ServiceBusReceivedMessage>>(
            "Resending",
            "Resent",
            "The count of resent messages was greater than the initial deadletter count. This may happen due to deadletters being re-sent and ending up on the deadletter queue again before the resend job was able to finish. It is an indicator that there are bad messages on your deadletter queue that should be handled and/or removed instead of resent. ",
            messagesCountToResend,
            resendMessagesWorkload);
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

                var deadletterCount = await _serviceBusService.GetDeadLetterCount(fullyQualifiedNamespace, entityPath);
                if (deadletterCount is null or 0)
                {
                    _consoleService.WriteError($"No deadletter messages found in queue {entityPath}");
                    return;
                }

                messagesCountToPeek = await _consoleService.PromptForLong(
                    $"Enter the [green]number of deadletter messages[/] to peek (max: {queues.First(q => q.QueueProperties.Name == entityPath).QueueRuntimeProperties.DeadLetterMessageCount}):",
                    minValue: 1,
                    maxValue: queues.First(q => q.QueueProperties.Name == entityPath).QueueRuntimeProperties.DeadLetterMessageCount);

                if (messagesCountToPeek == 0)
                    messagesCountToPeek = deadletterCount.Value;

                _consoleService.WriteMarkup($"[grey]Selected queue: {entityPath}[/]");

                break;
        }

        if (messagesCountToPeek > int.MaxValue)
            messagesCountToPeek = int.MaxValue;

        var peekWorkload = async () =>
        {
            return await _serviceBusService.PeekDeadLetterMessages(fullyQualifiedNamespace, entityPath, (int)messagesCountToPeek).ConfigureAwait(false);
        };

        var messages = await _consoleService.ProcessWorkloadWithSpinner(
            $"Peeking deadletter messages on {entityPath}...",
            peekWorkload);

        if (messages.Count == 0)
        {
            _consoleService.WriteError($"No deadletter messages found in queue {entityPath}");
            return;
        }

        var jsonMessages = messages.Select(m => new
        {
            m.MessageId,
            Body = m.Body?.ToString(),
            m.Subject,
            m.ContentType,
            m.CorrelationId,
            m.DeadLetterReason,
            m.DeadLetterErrorDescription,
            m.DeadLetterSource,
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

                var purgeMessagesWorkload = async () =>
                {
                    return await _serviceBusService.GetInformationAboutAllQueues(fullyQualifiedNamespace).ConfigureAwait(false);
                };

                var queues = await _consoleService.ProcessWorkloadWithSpinner($"Fetching queues on {fullyQualifiedNamespace}...", purgeMessagesWorkload);

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

        var deleteMessagesWorkload = async () =>
        {
            return await queue.DeadletterReceiver.ReceiveMessagesAsync(1000, TimeSpan.FromSeconds(30)); //Simply receiving messages deletes them as well
        };

        await _consoleService.ProcessWorkloadWithStatusUpdates<ServiceBusReceivedMessage, IReadOnlyList<ServiceBusReceivedMessage>>(
            "Deleting",
            "Deleted",
            "This is usually a sign that there are new deadletter messages arriving while purging. It might be good idea to investigate why this is happening.",
            deadletterCountTotal.Value,
            deleteMessagesWorkload);
    }
}
