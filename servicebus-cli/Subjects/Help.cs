
namespace servicebus_cli.Subjects;

public interface IHelp
{
    void Run();
}

public class Help : IHelp
{
    public void Run()
    {
        Console.WriteLine("Syntax: servicebus-cli <subject> <action> <parameter1> <parameterX> ... \n" +
                          "\n" +
                          "The following subjects and actions are available: \n" +
                          " - deadletter \n" +
                          "    - resend \n" +
                          "        - <FullyQualifiedNamespace> \n" +
                          "        - <EnitityPath> \n" + 
                          "        - <UseSessions> (Y/N) \n" +
                          " - namespace \n" +
                          "    - list \n" +
                          "        - <FullyQualifiedNamespace> \n" +
                          "        - <Filter> \n" +
                          "Example: servicebus-cli deadletter resend <FullyQualifiedNamespace> <EnitityPath>");
    }
}
