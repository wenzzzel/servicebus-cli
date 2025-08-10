using servicebus_cli.Subjects;
using Spectre.Console;

namespace servicebus_cli;

public interface IServiceBusCli
{
    Task Run(string[] args);
}

public class ServiceBusCli : IServiceBusCli
{
    private IDeadletter _deadletter;
    private IQueue queue;
    private IHelp _help;

    public ServiceBusCli(IDeadletter deadletter, IHelp help, IQueue queue)
    {
        _help = help;
        _deadletter = deadletter;
        this.queue = queue;
    }

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
                await queue.Run(args.Skip(1).ToArray());
                break;
            default:
                _help.Run();
                break;
        }
    }
}
