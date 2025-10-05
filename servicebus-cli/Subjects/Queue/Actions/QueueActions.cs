using servicebus_cli.Services;
using Spectre.Console;

namespace servicebus_cli.Subjects.Queue.Actions;

public interface IQueueActions
{
    Task List(List<string> args);
}

public class QueueActions(
    IServiceBusService serviceBusRepostitory,
    IFileService fileService,
    IUserSettingsService settingsService) : IQueueActions
{
    private readonly IServiceBusService _serviceBusService = serviceBusRepostitory;
    private readonly IFileService _fileService = fileService;
    private readonly IUserSettingsService _userSettingsService = settingsService;

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
                    fullyQualifiedNamespace = await AnsiConsole.PromptAsync(
                        new TextPrompt<string>("Enter the [green]fully qualified namespace[/]:")
                    );
                }
                else
                {
                    fullyQualifiedNamespace = await AnsiConsole.PromptAsync(
                        new SelectionPrompt<string>()
                            .Title("Select a fully qualified namespace:")
                            .PageSize(10)
                            .AddChoices(savedNamespaces.FullyQualifiedNamespaces)
                    );
                    AnsiConsole.MarkupLine($"[grey]Selected fully qualified namespace: {fullyQualifiedNamespace}[/]");
                }

                filter = await AnsiConsole.PromptAsync(
                    new TextPrompt<string>("Enter a [green]filter[/] (optional):")
                        .AllowEmpty()
                );
                break;
        }

        //TODO: Investigate if the table can be built in a status block to show progress

        var table = await AnsiConsole.Status()
            .StartAsync($"Listing queues on {fullyQualifiedNamespace}...", async ctx => 
            {
                ctx.Spinner(Spinner.Known.Dots);
                ctx.SpinnerStyle(Style.Parse("yellow"));
                
                var queuesWithInformation = await _serviceBusService.GetInformationAboutAllQueues(fullyQualifiedNamespace, filter).ConfigureAwait(false);
                
                ctx.Status("Building table...");
                
                var resultTable = new Table();
                resultTable.AddColumn("📮 [bold]Queue Name[/]");
                resultTable.AddColumn("[green]Active[/]");
                resultTable.AddColumn("[red]Dead Letter[/]");
                resultTable.AddColumn("[blue]Scheduled[/]");

                foreach (var queueInfo in queuesWithInformation)
                {
                    var activeMessageCount = queueInfo.QueueRuntimeProperties.ActiveMessageCount;
                    var deadLetterMessageCount = queueInfo.QueueRuntimeProperties.DeadLetterMessageCount;
                    var scheduledMessageCount = queueInfo.QueueRuntimeProperties.ScheduledMessageCount;

                    resultTable.AddRow(
                        queueInfo.QueueProperties.Name,
                        $"[green]{activeMessageCount}[/]",
                        $"[red]{deadLetterMessageCount}[/]",
                        $"[blue]{scheduledMessageCount}[/]"
                    );
                }
                
                return resultTable;
            });

        AnsiConsole.Write(table);
    }
}
