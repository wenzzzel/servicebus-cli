using servicebus_cli.Services;

namespace servicebus_cli.Subjects;

public interface IDeadletter
{
    Task Run(string[] args);
}

public class Deadletter : IDeadletter
{
    private readonly IHelp _helpService;
    private readonly IServiceBusService _serviceBusRepostitory;

    public Deadletter(IHelp helpService, IServiceBusService serviceBusRepostitory)
    {
        _helpService = helpService;
        _serviceBusRepostitory = serviceBusRepostitory;
    }

    public async Task Run(string[] args)
    {
        Console.WriteLine(">deadletter");
        if (args.Length < 1)
        {
            _helpService.Run();
            return;
        }

        switch (args[0])
        {
            case "resend":
                await Resend(args.Skip(1).ToList());
                break;
            case "purge":
                await Purge(args.Skip(1).ToList());
                break;
            default:
                _helpService.Run();
                break;
        }
    }

    private async Task Resend(List<string> args)
    {
        var fullyQualifiedNamespace = "";
        var entityPath = "";
        var useSession = "";

        switch (args.Count)
        {
            case 2:
                fullyQualifiedNamespace = args[0];
                entityPath = args[1];
                break;
            case 3:
                fullyQualifiedNamespace = args[0];
                entityPath = args[1];
                useSession = args[2];
                break;
            default:
                fullyQualifiedNamespace = await AnsiConsole.PromptAsync(
                    new TextPrompt<string>("Enter the [green]fully qualified namespace[/]:")
                );

                var queues = await _serviceBusRepostitory.GetQueues(fullyQualifiedNamespace);
                var selectedQueue = await AnsiConsole.PromptAsync(
                    new SelectionPrompt<string>()
                        .Title("Select a [green]queue[/]:")
                        .PageSize(30)
                        .EnableSearch()
                        .AddChoices(queues.Select(q => $"{q.QueueProperties.Name} ([green]{q.QueueRuntimeProperties.ActiveMessageCount}[/], [red]{q.QueueRuntimeProperties.DeadLetterMessageCount}[/], [blue]{q.QueueRuntimeProperties.ScheduledMessageCount}[/])").ToList())
                );
                entityPath = selectedQueue.Split(' ')[0];
                useSession = await AnsiConsole.ConfirmAsync("Use session?");
                break;
        }

        var confirmed = await AnsiConsole.ConfirmAsync($"[red]WARNING:[/] This action will resend all deadletter messages. Stopping the application before it's finished may result in data loss! Do you want to continue?");

        if (!confirmed)
        {
            AnsiConsole.MarkupLine("[red]Operation cancelled.[/]");
            return;
        }

        if (useSession != "Y")
            useSession = "N";

        Console.WriteLine($">resend fullyQualifiedNamespace: {fullyQualifiedNamespace}, entityPath: {entityPath}, useSessions: {useSession}");
        
        await _serviceBusRepostitory.ResendDeadletterMessage(fullyQualifiedNamespace, entityPath, useSession);
    }

    private async Task Purge(List<string> args)
    {
        var fullyQualifiedNamespace = "";
        var entityPath = "";

        switch (args.Count)
        {
            case 2:
                fullyQualifiedNamespace = args[0];
                entityPath = args[1];
                break;
            default:
                fullyQualifiedNamespace = await AnsiConsole.PromptAsync(
                    new TextPrompt<string>("Enter the [green]fully qualified namespace[/]:")
                );
                var queues = await _serviceBusRepostitory.GetQueues(fullyQualifiedNamespace);
                var selectedQueue = await AnsiConsole.PromptAsync(
                    new SelectionPrompt<string>()
                        .Title("Select a [green]queue[/]:")
                        .PageSize(30)
                        .EnableSearch()
                        .AddChoices(queues.Select(q => $"{q.QueueProperties.Name} ([green]{q.QueueRuntimeProperties.ActiveMessageCount}[/], [red]{q.QueueRuntimeProperties.DeadLetterMessageCount}[/], [blue]{q.QueueRuntimeProperties.ScheduledMessageCount}[/])").ToList())
                );
                entityPath = selectedQueue.Split(' ')[0];
                break;
        }

        var confirmed = await AnsiConsole.ConfirmAsync($"[red]WARNING:[/] This action will purge all deadletter messages. Do you want to continue?");

        if (!confirmed)
        {
            AnsiConsole.MarkupLine("[red]Operation cancelled.[/]");
            return;
        }

        Console.WriteLine($">purge fullyQualifiedNamespace: {fullyQualifiedNamespace}, entityPath: {entityPath}");
        
        await _serviceBusRepostitory.PurgeDeadletterQueue(fullyQualifiedNamespace, entityPath);
    }
}
