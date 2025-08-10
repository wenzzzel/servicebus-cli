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

    public Queue(IHelp helpService, IServiceBusService serviceBusRepostitory)
    {
        _helpService = helpService;
        _serviceBusService = serviceBusRepostitory;
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
                        "list",
                        "show"
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
                fullyQualifiedNamespace = await AnsiConsole.PromptAsync(
                    new TextPrompt<string>("Enter the [green]fully qualified namespace[/]:")
                );
                filter = await AnsiConsole.PromptAsync(
                    new TextPrompt<string>("Enter a [green]filter[/] (optional):")
                        .AllowEmpty()
                );
                break;
        }

        var queues = await _serviceBusService.GetQueues(fullyQualifiedNamespace, filter);

        AnsiConsole.MarkupLine($"[grey]Listing queues on {fullyQualifiedNamespace}...[/]");
        
        var table = new Table();
        table.AddColumn("📮 [bold]Queue Name[/]");
        table.AddColumn("[green]Active[/]");
        table.AddColumn("[red]Dead Letter[/]");
        table.AddColumn("[blue]Scheduled[/]");

        foreach (var queue in queues)
        {
            var activeMessageCount = queue.QueueRuntimeProperties.ActiveMessageCount;
            var deadLetterMessageCount = queue.QueueRuntimeProperties.DeadLetterMessageCount;
            var scheduledMessageCount = queue.QueueRuntimeProperties.ScheduledMessageCount;

            table.AddRow(
                queue.QueueProperties.Name,
                $"[green]{activeMessageCount}[/]",
                $"[red]{deadLetterMessageCount}[/]",
                $"[blue]{scheduledMessageCount}[/]"
            );
        }

        AnsiConsole.Write(table);
    }
}
