﻿
using servicebus_cli.Services;

namespace servicebus_cli.Subjects;

public interface IQueue
{
    Task Run(string[] args);
}

public class Queue : IQueue
{
    private readonly IHelp _helpService;
    private readonly IServiceBusService _serviceBusService;

    public Queue(IHelp helpService, IServiceBusService serviceBusRepostitory)
    {
        _helpService = helpService;
        _serviceBusService = serviceBusRepostitory;
    }

    public async Task Run(string[] args)
    {
        Console.WriteLine(">queue");
        if (args.Length < 1)
        {
            _helpService.Run();
            return;
        }

        switch (args[0])
        {
            case "list":
                await List(args.Skip(1).ToList());
                break;
            case "show":
                await Show(args.Skip(1).ToList());
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
        {
            _helpService.Run();
            return;
        }

        Console.WriteLine($">list fullyQualifiedNamespace: {fullyQualifiedNamespace}, filter: {filter}");

        await _serviceBusService.ListQueues(fullyQualifiedNamespace, filter);
    }

    private async Task Show(List<string> args)
    {
        var fullyQualifiedNamespace = "";
        var queueName = "";

        switch (args.Count)
        {
            case 1:
                fullyQualifiedNamespace = args[0];
                break;
            case 2:
                fullyQualifiedNamespace = args[0];
                queueName = args[1];
                break;
            default:
                _helpService.Run();
                return;
        }

        if (string.IsNullOrEmpty(fullyQualifiedNamespace))
        {
            _helpService.Run();
            return;
        }

        Console.WriteLine($">show fullyQualifiedNamespace: {fullyQualifiedNamespace}, queue: {queueName}");

        await _serviceBusService.ShowQueue(fullyQualifiedNamespace, queueName);
    }
}
