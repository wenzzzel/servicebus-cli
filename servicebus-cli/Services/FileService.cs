namespace servicebus_cli.Services;

public interface IFileService
{
    string GetConfigFileContent();
    void SetConfigFileContent(string content);
}

public class FileService() : IFileService
{
    public string GetConfigFileContent()
    {
        var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var configFilePath = Path.Combine(homePath, ".servicebus-cli", "config.json"); //TODO: Is this working on Windows?

        if (!File.Exists(configFilePath))
        {
            return string.Empty;
        }

        return File.ReadAllText(configFilePath);
    }

    public void SetConfigFileContent(string content)
    {
        var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var configDirectoryPath = Path.Combine(homePath, ".servicebus-cli");
        var configFilePath = Path.Combine(configDirectoryPath, "config.json"); //TODO: Is this working on Windows?

        if (!Directory.Exists(configDirectoryPath))
        {
            Directory.CreateDirectory(configDirectoryPath);
        }

        File.WriteAllText(configFilePath, content);
    }
}
