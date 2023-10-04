using servicebus_cli.Subjects;

namespace servicebus_cli;

public interface IServiceBusCli
{
    Task Run(string[] args);
}

public class ServiceBusCli : IServiceBusCli
{
    private IDeadletter _deadletter;
    private IHelp _help;

    public ServiceBusCli(IDeadletter deadletter, IHelp help)
    {

        _help = help;
        _deadletter = deadletter;
    }

    public async Task Run(string[] args)
    {
        if (args.Length == 0)
            _help.Run();
        else
            switch (args[0])
            {
                case "deadletter":
                    await _deadletter.Run(args.Skip(1).ToArray());
                    break;
                default:
                    _help.Run();
                    break;
            }
    }
}
