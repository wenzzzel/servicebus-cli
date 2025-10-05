using servicebus_cli.Services;
using servicebus_cli.Subjects.Deadletter.Actions;

namespace servicebus_cli.Subjects.Deadletter;

public interface IDeadletter
{
    Task Run(string[] args);
}

public class Deadletter(IHelp _helpSubject, IDeadletterActions _deadletterActions, IConsoleService _consoleService) : IDeadletter
{
    public async Task Run(string[] args)
    {
        string selectedAction = "";
        if (args.Length < 1)
        {
            selectedAction = await _consoleService.PromptForAction<DeadletterActions>();
        }
        else
        {
            selectedAction = args[0];
        }

        _consoleService.WriteMarkup($"[grey]Selected action: {selectedAction}[/]");

        switch (selectedAction)
        {
            case "resend":
                await _deadletterActions.Resend(args.Skip(1).ToList());
                break;
            case "purge":
                await _deadletterActions.Purge(args.Skip(1).ToList());
                break;
            default:
                _helpSubject.Run();
                break;
        }
    }
}
