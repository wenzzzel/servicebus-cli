using servicebus_cli.Services;
using Spectre.Console;

namespace servicebus_cli.Subjects;

public interface IQueue
{
    Task Run(string[] args);
}

public class Queue : IQueue
{
    private readonly IHelp _helpService;
    private readonly IServiceBusService _serviceBusService;
    private readonly IFileService _fileService;
    private readonly IUserSettingsService _userSettingsService;

    public Queue(IHelp helpService, IServiceBusService serviceBusRepostitory, IFileService fileService, IUserSettingsService settingsService)
    {
        _helpService = helpService;
        _fileService = fileService;
        _serviceBusService = serviceBusRepostitory;
        _userSettingsService = settingsService;
    }

    public async Task Run(string[] args)
    {
        string selectedAction;
        if (args.Length < 1)
        {
            selectedAction = await AnsiConsole.PromptAsync(
                new SelectionPrompt<string>()
                    .Title("Action: ")
                    .PageSize(10)
                    .AddChoices(
                        "list"
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
            case "list":
                await List(args.Skip(1).ToList());
                break;
            default:
                _helpService.Run();
                break;
        }
    }

    private async Task List(List<string> args)
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
