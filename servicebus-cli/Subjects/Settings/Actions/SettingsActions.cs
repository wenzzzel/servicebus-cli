using servicebus_cli.Models;
using servicebus_cli.Services;
using Spectre.Console;

namespace servicebus_cli.Subjects.Settings;

public interface ISettingsActions
{
    Task Get(string[] args);
    Task Set(string[] args);
}

public class SettingsActions(
    IHelp helpService,
    IUserSettingsService userSettingsService,
    IFileService fileService) : ISettingsActions
{
    private readonly IHelp _helpService = helpService;
    private readonly IUserSettingsService _userSettingsService = userSettingsService;
    private readonly IFileService _fileService = fileService;

    public async Task Get(string[] args)
    {
        string selectedSetting = "";
        if (args.Length < 1)
        {
            selectedSetting = await AnsiConsole.PromptAsync(
                new SelectionPrompt<string>()
                    .Title("Setting: ")
                    .PageSize(10)
                    .AddChoices(
                        "fullyQualifiedNamespaces"
                    )
            );
        }
        else
        {
            selectedSetting = args[0];
        }

        var settingsContent = _fileService.GetConfigFileContent();
        var userSettings = _userSettingsService.Deserialize(settingsContent);

        AnsiConsole.MarkupLine($"[grey]Selected setting: {selectedSetting}[/]");
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
            AnsiConsole.MarkupLine($"[green]{fqns}[/]");
        }
    }

    public async Task Set(string[] args)
    {
        string selectedSetting = "";
        if (args.Length < 1)
        {
            selectedSetting = await AnsiConsole.PromptAsync(
                new SelectionPrompt<string>()
                    .Title("Setting: ")
                    .PageSize(10)
                    .AddChoices(
                        "fullyQualifiedNamespaces"
                    )
            );
        }
        else
        {
            selectedSetting = args[0];
        }

        var settingsContent = _fileService.GetConfigFileContent();
        var userSettings = _userSettingsService.Deserialize(settingsContent);

        AnsiConsole.MarkupLine($"[grey]Selected setting: {selectedSetting}[/]");
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
        var newFullyQualifiedNamespaces = await AnsiConsole.PromptAsync(
            new TextPrompt<string>("Enter [green]fully qualified namespace(s)[/] as comma separated values:")
        );

        var namespaces = newFullyQualifiedNamespaces.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();

        userSettings.FullyQualifiedNamespaces = namespaces;

        var settingsJson = _userSettingsService.Serialize(userSettings);

        _fileService.SetConfigFileContent(settingsJson);
    }
}
