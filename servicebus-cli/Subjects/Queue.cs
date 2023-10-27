
using servicebus_cli.Repositories;

namespace servicebus_cli.Subjects;

public interface IQueue
{
    Task Run(string[] args);
}

public class Queue : IQueue
{
    private readonly IHelp _helpService;
    private readonly IServiceBusRepostitory _serviceBusRepostitory;

    public Queue(IHelp helpService, IServiceBusRepostitory serviceBusRepostitory)
    {
        _helpService = helpService;
        _serviceBusRepostitory = serviceBusRepostitory;
    }

    public async Task Run(string[] args)
    {
        Console.WriteLine(">queue");
        if (args.Length is not 2 and not 3)
        {
            _helpService.Run();
            return;
        }

        switch (args[0])
        {
            case "list":
                await List(args.Skip(1).ToList());
                break;
            default:
                _helpService.Run();
                break;
        }
    }

    private async Task List(List<string> args)
    {
        var fullyQualifiedNamespace = "";
        var filter = "";

        switch (args.Count)
        {
            case 1:
                fullyQualifiedNamespace = args[0];
                break;
            case 2:
                fullyQualifiedNamespace = args[0];
                filter = args[1];
                break;
            default:
                _helpService.Run();
                return;
        }

        if (string.IsNullOrEmpty(fullyQualifiedNamespace))
            _helpService.Run();

        Console.WriteLine($">list fullyQualifiedNamespace: {fullyQualifiedNamespace}, filter: {filter}");

        await _serviceBusRepostitory.ListQueues(fullyQualifiedNamespace, filter);
    }
}
