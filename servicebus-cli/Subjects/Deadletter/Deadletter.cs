using servicebus_cli.Subjects.Deadletter.Actions;
using Spectre.Console;

namespace servicebus_cli.Subjects.Deadletter;

public interface IDeadletter
{
    Task Run(string[] args);
}

public class Deadletter(IHelp _helpSubject, IDeadletterActions _deadletterActions) : IDeadletter
{
    public async Task Run(string[] args)
    {
        string selectedAction = "";
        if (args.Length < 1)
        {
            selectedAction = await AnsiConsole.PromptAsync(
                new SelectionPrompt<string>()
                    .Title("Action: ")
                    .PageSize(10)
                    .AddChoices(
                        "resend",
                        "purge"
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
