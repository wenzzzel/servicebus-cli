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
                _helpService.Run();
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
                _helpService.Run();
                return;
        }

        Console.WriteLine($">purge fullyQualifiedNamespace: {fullyQualifiedNamespace}, entityPath: {entityPath}");
        
        await _serviceBusRepostitory.PurgeDeadletterQueue(fullyQualifiedNamespace, entityPath);
    }
}
