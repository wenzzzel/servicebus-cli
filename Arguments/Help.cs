
namespace servicebus_cli.Arguments;

internal static class Help
{
    public static void Main()
    {
        Console.WriteLine("" +
            "Syntax: servicebus-cli <subject> <action> <parameter1> <parameter2> ... \n" +
            "Example: servicebus-cli deadletter resend <FullyQualifiedNamespace> <EnitityPath>");
    }
}
