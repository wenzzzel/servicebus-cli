using Azure.Identity;
using Azure.Messaging.ServiceBus;
using servicebus_cli.Repositories;

namespace servicebus_cli.Subjects;

public interface IDeadletter
{
    Task Run(string[] args);
}

public class Deadletter : IDeadletter
{
    private readonly IHelp _helpService;
    private readonly IServiceBusRepostitory _serviceBusRepostitory;

    public Deadletter(IHelp helpService, IServiceBusRepostitory serviceBusRepostitory)
    {
        _helpService = helpService;
        _serviceBusRepostitory = serviceBusRepostitory;
    }

    public async Task Run(string[] args)
    {
        Console.WriteLine(">deadletter");
        if (args.Length is not 3 and not 4)
        {
            _helpService.Run();
            return;
        }

        switch (args[0])
        {
            case "resend":
                await Resend(args.Skip(1).ToList());
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

        if (args.Count == 2)
        {
            fullyQualifiedNamespace = args[0];
            entityPath = args[1];
        }
        else if (args.Count == 3) 
        {
            fullyQualifiedNamespace = args[0];
            entityPath = args[1];
            useSession = args[2];
        }
        else
        {
            _helpService.Run();
            return;
        }

        if (useSession != "N" && useSession != "Y")
            useSession = "N";

        Console.WriteLine($">resend fullyQualifiedNamespace: {fullyQualifiedNamespace}, entityPath: {entityPath}, useSessions: {useSession}");
        
        await _serviceBusRepostitory.ResendDeadletterMessage(fullyQualifiedNamespace, entityPath, useSession);
    }
}
