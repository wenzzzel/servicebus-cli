using Microsoft.Extensions.DependencyInjection;
using servicebus_cli.Subjects;

namespace servicebus_cli;

public class Program
{
    public static async Task Main(string[] args)
    {
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IDeadletter, Deadletter>()
            .AddSingleton<IHelp, Help>()
            .BuildServiceProvider();

        var deadletter = serviceProvider.GetService<IDeadletter>();
        var help = serviceProvider.GetService<IHelp>();

        if (args.Length == 0)
            help.Run();
        else
            switch (args[0])
            {
                case "deadletter":
                    await deadletter.Run(args.Skip(1).ToArray());
                    break;
                default:
                    help.Run();
                    break;
            }
    }
}