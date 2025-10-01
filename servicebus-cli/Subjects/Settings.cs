using servicebus_cli.Models;
using servicebus_cli.Services;
using Spectre.Console;

namespace servicebus_cli.Subjects;

public interface ISettings
{
    Task Run(string[] args);
}

public class Settings : ISettings
{
    private readonly IHelp _helpService;
    private readonly IUserSettingsService _userSettingsService;
    private readonly IFileService _fileService;

    public Settings(IHelp helpService, IUserSettingsService userSettingsService, IFileService fileService)
    {
        _helpService = helpService;
        _userSettingsService = userSettingsService;
        _fileService = fileService;
    }

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
                await Get(args.Skip(1).ToArray());
                break;
            case "set":
                await Set(args.Skip(1).ToArray());
                break;
            default:
                _helpService.Run();
                break;
        }
    }

    private async Task Get(string[] args)
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

    private async Task Set(string[] args)
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
