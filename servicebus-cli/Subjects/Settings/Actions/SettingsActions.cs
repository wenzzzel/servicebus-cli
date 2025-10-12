using servicebus_cli.Models;
using servicebus_cli.Services;

namespace servicebus_cli.Subjects.Settings;

public interface ISettingsActions
{
    Task Get(string[] args);
    Task Set(string[] args);
}

public class SettingsActions(
    IHelp _helpService,
    IUserSettingsService _userSettingsService,
    IFileService _fileService,
    IConsoleService _consoleService) : ISettingsActions
{
    public async Task Get(string[] args)
    {
        string selectedSetting = "";
        if (args.Length < 1)
        {
            selectedSetting = await _consoleService.PromptSelection("Setting: ", new[] { "fullyQualifiedNamespaces" });
        }
        else
        {
            selectedSetting = args[0];
        }

        var settingsContent = _fileService.GetConfigFileContent();
        var userSettings = _userSettingsService.Deserialize(settingsContent);

        _consoleService.WriteMarkup($"[grey]Selected setting: {selectedSetting}[/]");

        switch (selectedSetting)
        {
            case "fullyQualifiedNamespaces":
                await PrintFullyQualifiedNamespaces(userSettings.FullyQualifiedNamespaces);
                break;
            default:
                _helpService.Run();
                break;
        }
    }

    private async Task PrintFullyQualifiedNamespaces(List<string> fullyQualifiedNamespaces)
    {
        foreach (var fqns in fullyQualifiedNamespaces)
        {
            _consoleService.WriteMarkup($"[green]{fqns}[/]");
        }
    }

    public async Task Set(string[] args)
    {
        string selectedSetting = "";
        if (args.Length < 1)
        {
            selectedSetting = await _consoleService.PromptSelection("Setting: ", new[] { "fullyQualifiedNamespaces" });
        }
        else
        {
            selectedSetting = args[0];
        }

        var settingsContent = _fileService.GetConfigFileContent();
        var userSettings = _userSettingsService.Deserialize(settingsContent);

        _consoleService.WriteMarkup($"[grey]Current settings:[/]");
        switch (selectedSetting)
        {
            case "fullyQualifiedNamespaces":
                await SetFullyQualifiedNamespaces(userSettings);
                break;
            default:
                _helpService.Run();
                break;
        }
    }

    private async Task SetFullyQualifiedNamespaces(UserSettings userSettings)
    {
        var newFullyQualifiedNamespaces = await _consoleService.PromptFreeText("Enter [green]fully qualified namespace(s)[/] as comma separated values:");

        var namespaces = newFullyQualifiedNamespaces.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();

        userSettings.FullyQualifiedNamespaces = namespaces;

        var settingsJson = _userSettingsService.Serialize(userSettings);

        _fileService.SetConfigFileContent(settingsJson);
    }
}
