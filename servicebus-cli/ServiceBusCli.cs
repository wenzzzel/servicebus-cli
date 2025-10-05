using servicebus_cli.Subjects;
using servicebus_cli.Subjects.Deadletter;
using servicebus_cli.Subjects.Queue;
using servicebus_cli.Subjects.Settings;
using Spectre.Console;

namespace servicebus_cli;

public interface IServiceBusCli
{
    Task Run(string[] args);
}

public class ServiceBusCli(
    IDeadletter deadletter,
    IHelp help,
    IQueue queue,
    ISettings settings) : IServiceBusCli
{
    private readonly IDeadletter _deadletter = deadletter;
    private readonly IQueue _queue = queue;
    private readonly IHelp _help = help;
    private readonly ISettings _settings = settings;

    public async Task Run(string[] args)
    {
        string selectedSubject;

        if (args.Length == 0)
        {
            selectedSubject = await AnsiConsole.PromptAsync(
                new SelectionPrompt<string>()
                    .Title("Subject: ")
                    .PageSize(10)
                    .AddChoices(
                        "deadletter",
                        "queue",
                        "settings",
                        "help"
                    )
            );
        }
        else
        {
            selectedSubject = args[0];
        }

        AnsiConsole.MarkupLine($"[grey]Selected subject: {selectedSubject}[/]");

        switch (selectedSubject)
        {
            case "deadletter":
                await _deadletter.Run(args.Skip(1).ToArray());
                break;
            case "queue":
                await _queue.Run(args.Skip(1).ToArray());
                break;
            case "settings":
                await _settings.Run(args.Skip(1).ToArray());
                break;
            default:
                _help.Run();
                break;
        }
    }
}
