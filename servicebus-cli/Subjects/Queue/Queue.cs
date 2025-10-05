using servicebus_cli.Services;
using servicebus_cli.Subjects.Queue.Actions;
using Spectre.Console;

namespace servicebus_cli.Subjects.Queue;

public interface IQueue
{
    Task Run(string[] args);
}

public class Queue(
    IHelp helpService,
    IServiceBusService serviceBusRepostitory,
    IFileService fileService,
    IUserSettingsService settingsService,
    IQueueActions queueActions) : IQueue
{
    private readonly IHelp _helpService = helpService;
    private readonly IServiceBusService _serviceBusService = serviceBusRepostitory;
    private readonly IFileService _fileService = fileService;
    private readonly IUserSettingsService _userSettingsService = settingsService;
    private readonly IQueueActions _queueActions = queueActions;

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
                _helpService.Run();
                break;
        }
    }
}
