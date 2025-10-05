using servicebus_cli.Services;

namespace servicebus_cli.Subjects.Settings;

public interface ISettings
{
    Task Run(string[] args);
}

public class Settings(IHelp _helpSubject, ISettingsActions _settingsActions, IConsoleService _consoleService) : ISettings
{
    public async Task Run(string[] args)
    {
        string selectedAction = "";
        if (args.Length < 1)
            selectedAction = await _consoleService.PromptForAction<SettingsActions>();
        else
            selectedAction = args[0];

        _consoleService.WriteMarkup($"[grey]Selected action: {selectedAction}[/]");

        switch (selectedAction)
        {
            case "get":
                await _settingsActions.Get(args.Skip(1).ToArray());
                break;
            case "set":
                await _settingsActions.Set(args.Skip(1).ToArray());
                break;
            default:
                _helpSubject.Run();
                break;
        }
    }
}
