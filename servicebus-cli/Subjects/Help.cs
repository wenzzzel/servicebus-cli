
namespace servicebus_cli.Subjects;

public static class Help
{
    public static void Run()
    {
        Console.WriteLine("Syntax: servicebus-cli <subject> <action> <parameter1> <parameterX> ... \n" +
                          "\n" +
                          "The following subjects and actions are available: \n" +
                          " - deadletter \n" +
                          "    - resend \n" +
                          "        - <FullyQualifiedNamespace> \n" +
                          "        - <EnitityPath> \n" + 
                          "Example: servicebus-cli deadletter resend <FullyQualifiedNamespace> <EnitityPath>");
    }
}
