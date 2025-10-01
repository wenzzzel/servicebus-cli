using servicebus_cli.Models;

namespace servicebus_cli.Services;

public interface IUserSettingsService
{
    string Serialize(UserSettings settings);
    UserSettings Deserialize(string content);
}

public class UserSettingsService() : IUserSettingsService
{
    public string Serialize(UserSettings settings)
    {
        return System.Text.Json.JsonSerializer.Serialize(
            settings,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }

    public UserSettings Deserialize(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return new UserSettings();
        }

        return System.Text.Json.JsonSerializer.Deserialize<UserSettings>(content) ?? new UserSettings();
    }
}
