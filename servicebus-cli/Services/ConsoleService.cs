using servicebus_cli.Subjects.Deadletter.Actions;
using servicebus_cli.Subjects.Queue.Actions;
using servicebus_cli.Subjects.Settings;
using Spectre.Console;

namespace servicebus_cli.Services;

public interface IConsoleService
{
    void WriteMarkup(string markup);
    Task<string> PromptForSubject();
    Task<string> PromptForAction<ActionType>();
    Task<string> PromptSelection(string title, IEnumerable<string> choices);
}

public class ConsoleService() : IConsoleService
{
    public void WriteMarkup(string markup)
    {
        AnsiConsole.MarkupLine(markup);
    }

    public Task<string> PromptForSubject() => PromptSelection("Subject: ", ["deadletter", "queue", "settings", "help"]);
    public Task<string> PromptForAction<ActionType>()
    {
        if (typeof(ActionType) == typeof(DeadletterActions))
            return PromptSelection("Action: ", ["resend", "purge"]);

        if (typeof(ActionType) == typeof(QueueActions))
            return PromptSelection("Action: ", ["list"]);

        if (typeof(ActionType) == typeof(SettingsActions))
            return PromptSelection("Action: ", ["get", "set"]); //TODO: Add list action

        return Task.FromResult("help");
    } 

    public Task<string> PromptSelection(string title, IEnumerable<string> choices)
    {
        return AnsiConsole.PromptAsync(
            new SelectionPrompt<string>()
                .Title(title)
                .PageSize(10)
                .AddChoices(choices)
        );
    }
}
