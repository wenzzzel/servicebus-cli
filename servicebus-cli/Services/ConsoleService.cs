using Azure.Messaging.ServiceBus;
using servicebus_cli.Subjects.Deadletter.Actions;
using servicebus_cli.Subjects.Queue.Actions;
using servicebus_cli.Subjects.Settings;
using Spectre.Console;

namespace servicebus_cli.Services;

public interface IConsoleService
{
    Task<bool> ConfirmWarning(string message);
    void WriteMarkup(string markup);
    void WriteError(string markup);
    void WriteWarning(string markup);
    void WriteSuccess(string markup);
    Task<string> PromptForSubject();
    Task<string> PromptForAction<ActionType>();
    Task<string> PromptSelection(string title, IEnumerable<string> choices, bool enableSearch = false);
    Task<string> PromptFreeText(string title, bool allowEmpty = false);
    Task<T> ProcessWorkloadWithSpinner<T>(string message, Func<Task<T>> func);
    Task ProcessWorkloadWithStatusUpdates<TItem, TCollection>(
        string taskDescriptionCurrentTense,
        string taskDescriptionPastTense,
        string failMessage,
        long targetValue,
        Func<Task<TCollection>> func)
        where TCollection : IEnumerable<TItem>;
}

public class ConsoleService() : IConsoleService
{
    public Task<bool> ConfirmWarning(string message) => AnsiConsole.ConfirmAsync($"[yellow]Warning:[/] " + message);

    public void WriteError(string markup) => AnsiConsole.MarkupLine($"[red]Error:[/] " + markup);
    public void WriteWarning(string markup) => AnsiConsole.MarkupLine($"[yellow]Warning:[/] " + markup);
    public void WriteSuccess(string markup) => AnsiConsole.MarkupLine($"[green]Success:[/] " + markup);

    public void WriteMarkup(string markup) => AnsiConsole.MarkupLine(markup);

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

    public Task<string> PromptSelection(string title, IEnumerable<string> choices, bool enableSearch = false)
    {
        if (enableSearch)
        {
            return AnsiConsole.PromptAsync(
                new SelectionPrompt<string>()
                    .Title(title)
                    .PageSize(10)
                    .EnableSearch()
                    .AddChoices(choices)
            );
        }

        return AnsiConsole.PromptAsync(
            new SelectionPrompt<string>()
                .Title(title)
                .PageSize(10)
                .AddChoices(choices)
        );
    }

    public Task<string> PromptFreeText(string title, bool allowEmpty = false)
    {
        if (allowEmpty)
            return AnsiConsole.PromptAsync(new TextPrompt<string>(title).AllowEmpty());

        return AnsiConsole.PromptAsync(new TextPrompt<string>(title));
    }

    public Task<T> ProcessWorkloadWithSpinner<T>(string message, Func<Task<T>> func)
    {
        return AnsiConsole.Status()
            .StartAsync(message, async ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                ctx.SpinnerStyle(Style.Parse("yellow"));

                return await func().ConfigureAwait(false);
            });
    }
    
    public async Task ProcessWorkloadWithStatusUpdates<TItem, TCollection>(
        string taskDescriptionCurrentTense,
        string taskDescriptionPastTense,
        string failMessage,
        long targetValue,
        Func<Task<TCollection>> func)
        where TCollection : IEnumerable<TItem>
    {
        var currentValue = 0L;

        var statusMessage = taskDescriptionCurrentTense + $" {currentValue} / {targetValue}";

        TCollection workloadResult;

        await AnsiConsole.Status().StartAsync(statusMessage, async ctx =>
        {
            do
            {
                workloadResult = await func().ConfigureAwait(false);

                currentValue += workloadResult.Count();

                statusMessage = taskDescriptionCurrentTense + $" {currentValue} / {targetValue}";
                ctx.Status(statusMessage);

            } while (workloadResult.Any() && currentValue < targetValue);
        });

        bool taskSucceeded = currentValue == targetValue;

        if (taskSucceeded)
        {
            WriteSuccess(taskDescriptionPastTense + " " + $"{currentValue} / {targetValue}.");
        }
        else
        {
            WriteWarning(taskDescriptionPastTense + " " + $"{currentValue} / {targetValue}. " + failMessage);
        }

    }
}
