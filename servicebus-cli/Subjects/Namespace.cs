
using servicebus_cli.Repositories;

namespace servicebus_cli.Subjects;

public interface INamespace
{
    Task Run(string[] args);
}

public class Namespace : INamespace
{
    private readonly IHelp _helpService;
    private readonly IServiceBusRepostitory _serviceBusRepostitory;

    public Namespace(IHelp helpService, IServiceBusRepostitory serviceBusRepostitory)
    {
        _helpService = helpService;
        _serviceBusRepostitory = serviceBusRepostitory;
    }

    public async Task Run(string[] args)
    {
        Console.WriteLine(">namespace");
        if (args.Length is not 1)
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

    }
}
