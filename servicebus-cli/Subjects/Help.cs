using Spectre.Console;

namespace servicebus_cli.Subjects;

public interface IHelp
{
    void Run();
}

public class Help : IHelp
{
    public void Run()
    {
        // Display the title with styling
        AnsiConsole.Write(
            new FigletText("ServiceBus CLI")
                .Centered()
                .Color(Color.Blue));

        AnsiConsole.WriteLine();
        
        // Display syntax information
        var syntaxPanel = new Panel("[bold]Syntax:[/] [cyan]servicebus-cli[/] [yellow]<subject>[/] [green]<action>[/] [dim]<parameter1>[/] [dim]<parameterX>[/] ...")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Grey)
            .Header("[bold blue]Command Syntax[/]");
        
        AnsiConsole.Write(syntaxPanel);
        AnsiConsole.WriteLine();

        // Create the command tree
        var tree = new Tree("[bold blue]Available Commands[/]")
            .Style(Style.Parse("blue"));

        // Deadletter branch
        var deadletterNode = tree.AddNode("[bold yellow]deadletter[/] - Dead letter queue operations");
        
        var resendNode = deadletterNode.AddNode("[green]resend[/] - Resend messages from dead letter queue");
        resendNode.AddNode("[dim]<FullyQualifiedNamespace>[/] - Service Bus namespace");
        resendNode.AddNode("[dim]<EntityPath>[/] - Queue or topic name");
        
        var purgeNode = deadletterNode.AddNode("[green]purge[/] - Remove all messages from dead letter queue");
        purgeNode.AddNode("[dim]<FullyQualifiedNamespace>[/] - Service Bus namespace");
        purgeNode.AddNode("[dim]<EntityPath>[/] - Queue or topic name");

        // Queue branch
        var queueNode = tree.AddNode("[bold yellow]queue[/] - Queue management operations");
        
        var listNode = queueNode.AddNode("[green]list[/] - List queues in namespace");
        listNode.AddNode("[dim]<FullyQualifiedNamespace>[/] - Service Bus namespace");
        listNode.AddNode("[dim]<Filter>[/] - Optional filter pattern");

        // Render the tree
        AnsiConsole.Write(tree);
        AnsiConsole.WriteLine();

        // Add example section
        var examplePanel = new Panel("[bold]Example:[/]\n[cyan]servicebus-cli[/] [yellow]deadletter[/] [green]resend[/] [dim]myservicebus.servicebus.windows.net myqueue[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green)
            .Header("[bold green]Usage Example[/]");
        
        AnsiConsole.Write(examplePanel);
    }
}
