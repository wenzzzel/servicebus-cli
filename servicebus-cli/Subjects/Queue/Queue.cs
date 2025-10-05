using servicebus_cli.Services;
using servicebus_cli.Subjects.Queue.Actions;

namespace servicebus_cli.Subjects.Queue;

public interface IQueue
{
    Task Run(string[] args);
}

public class Queue(IHelp _helpSubject, IQueueActions _queueActions, IConsoleService _consoleService) : IQueue
{
    public async Task Run(string[] args)
    {
        string selectedAction;
        if (args.Length < 1)
            selectedAction = await _consoleService.PromptForAction<QueueActions>();
        else
            selectedAction = args[0];

        _consoleService.WriteMarkup($"[grey]Selected action: {selectedAction}[/]");

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
