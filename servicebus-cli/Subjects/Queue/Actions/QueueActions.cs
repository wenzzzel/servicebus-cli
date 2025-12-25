using servicebus_cli.Services;

namespace servicebus_cli.Subjects.Queue.Actions;

public interface IQueueActions
{
    Task List(List<string> args);
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

                filter = await _consoleService.PromptFreeText("Enter a [green]filter[/] (optional):");

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
}
