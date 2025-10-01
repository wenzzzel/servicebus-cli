using Microsoft.Extensions.DependencyInjection;
using servicebus_cli.Repositories;
using servicebus_cli.Services;
using servicebus_cli.Subjects;

namespace servicebus_cli;

public class Program
{
    public static async Task Main(string[] args)
    {
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IServiceBusCli, ServiceBusCli>()
            .AddSingleton<IDeadletter, Deadletter>()
            .AddSingleton<ISettings, Settings>()
            .AddSingleton<IQueue, Queue>()
            .AddSingleton<IServiceBusService, ServiceBusService>()
            .AddSingleton<IFileService, FileService>()
            .AddSingleton<IUserSettingsService, UserSettingsService>()
            .AddSingleton<IServiceBusRepository, ServiceBusRepository>()
            .AddSingleton<IHelp, Help>()
            .BuildServiceProvider();

        var serviceBusCli = serviceProvider.GetService<IServiceBusCli>();
        await serviceBusCli.Run(args);
    }
}