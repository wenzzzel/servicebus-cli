using servicebus_cli.Subjects.Queue.Actions;
using Spectre.Console;

namespace servicebus_cli.Subjects.Queue;

public interface IQueue
{
    Task Run(string[] args);
}

public class Queue(IHelp _helpSubject, IQueueActions _queueActions) : IQueue
{
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
                await _queueActions.List(args.Skip(1).ToList());
                break;
            default:
                _helpSubject.Run();
                break;
        }
    }
}
