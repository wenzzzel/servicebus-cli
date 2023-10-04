using Microsoft.Extensions.DependencyInjection;
using servicebus_cli.Subjects;

namespace servicebus_cli;

public class Program
{
    public static async Task Main(string[] args)
    {
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IServiceBusCli, ServiceBusCli>()
            .AddSingleton<IDeadletter, Deadletter>()
            .AddSingleton<IHelp, Help>()
            .BuildServiceProvider();

        var serviceBusCli = serviceProvider.GetService<IServiceBusCli>();
        await serviceBusCli.Run(args);
    }
}