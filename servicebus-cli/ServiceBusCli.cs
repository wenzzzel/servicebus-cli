using servicebus_cli.Services;
using servicebus_cli.Subjects.Deadletter;
using servicebus_cli.Subjects.Queue;
using servicebus_cli.Subjects.Settings;

namespace servicebus_cli;

public interface IServiceBusCli
{
    Task Run(string[] args);
}

public class ServiceBusCli(
    IDeadletter _deadletter,
    IHelp _help,
    IQueue _queue,
    ISettings _settings,
    IConsoleService _consoleService) : IServiceBusCli
{
    public async Task Run(string[] args)
    {
        string selectedSubject;

        if (args.Length == 0)
            selectedSubject = await _consoleService.PromptForSubject();
        else
            selectedSubject = args[0];

        _consoleService.WriteMarkup($"[grey]Selected subject: {selectedSubject}[/]");

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
