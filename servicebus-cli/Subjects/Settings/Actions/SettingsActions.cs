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

    private const string FULLY_QUALIFIED_NAMESPACES = "fullyQualifiedNamespaces";
    private const string ADDITIONAL_COLUMNS = "additionalColumns";
    private readonly List<string> _availableSettings = new() { FULLY_QUALIFIED_NAMESPACES, ADDITIONAL_COLUMNS };

    public async Task Get(string[] args)
    {
        string selectedSetting = "";
        if (args.Length < 1)
        {
            selectedSetting = await _consoleService.PromptSelection("Setting: ", _availableSettings);
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
            case FULLY_QUALIFIED_NAMESPACES:
                await PrintFullyQualifiedNamespaces(userSettings.FullyQualifiedNamespaces);
                break;
            case ADDITIONAL_COLUMNS:
                await PrintAdditionalColumns(userSettings.AdditionalColumns);
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

    private async Task PrintAdditionalColumns(Dictionary<string, string> additionalColumns)
    {
        foreach (var column in additionalColumns)
        {
            _consoleService.WriteMarkup($"[green]{column.Key}[/]: [grey on grey15]{column.Value}[/]");
        }
    }

    public async Task Set(string[] args)
    {
        string selectedSetting = "";
        if (args.Length < 1)
        {
            selectedSetting = await _consoleService.PromptSelection("Setting: ", _availableSettings);
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
            case FULLY_QUALIFIED_NAMESPACES:
                await SetFullyQualifiedNamespaces(userSettings);
                break;
            case ADDITIONAL_COLUMNS:
                await SetAdditionalColumns(userSettings);
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

    private async Task SetAdditionalColumns(UserSettings userSettings)
    {
        var newAdditionalColumns = await _consoleService.PromptFreeText("Enter [green]additional column(s)[/] as comma separated values in the following format [grey on grey15]column1Name=path.to.metadata.field,column2Name=path.to.metadata.field[/]:");

        var columns = newAdditionalColumns.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();

        var additionalColumns = new Dictionary<string, string>();
        foreach (var column in columns)
        {
            var parts = column.Split('=');
            if (parts.Length == 2)
            {
                additionalColumns[parts[0].Trim()] = parts[1].Trim();
            }
        }

        userSettings.AdditionalColumns = additionalColumns;

        var settingsJson = _userSettingsService.Serialize(userSettings);

        _fileService.SetConfigFileContent(settingsJson);
    }
}
