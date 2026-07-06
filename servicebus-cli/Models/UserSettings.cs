namespace servicebus_cli.Models;

public class UserSettings
{
    public List<string> FullyQualifiedNamespaces { get; set; } = new List<string>();
    public Dictionary<string, string> AdditionalColumns { get; set; } = new Dictionary<string, string>();
}
