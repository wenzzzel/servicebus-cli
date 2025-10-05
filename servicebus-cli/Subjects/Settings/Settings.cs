using servicebus_cli.Services;
using Spectre.Console;

namespace servicebus_cli.Subjects.Settings;

public interface ISettings
{
    Task Run(string[] args);
}

public class Settings(
    IHelp helpService,
    IUserSettingsService userSettingsService,
    IFileService fileService,
    ISettingsActions settingsActions) : ISettings
{
    private readonly IHelp _helpService = helpService;
    private readonly IUserSettingsService _userSettingsService = userSettingsService;
    private readonly IFileService _fileService = fileService;
    private readonly ISettingsActions _settingsActions = settingsActions;

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
                        "get",
                        "set"
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
            case "get":
                await _settingsActions.Get(args.Skip(1).ToArray());
                break;
            case "set":
                await _settingsActions.Set(args.Skip(1).ToArray());
                break;
            default:
                _helpService.Run();
                break;
        }
    }
}
