using Microsoft.Extensions.DependencyInjection;
using servicebus_cli.Repositories;
using servicebus_cli.Services;
using servicebus_cli.Subjects;
using servicebus_cli.Subjects.Deadletter;
using servicebus_cli.Subjects.Deadletter.Actions;
using servicebus_cli.Subjects.Queue;
using servicebus_cli.Subjects.Queue.Actions;
using servicebus_cli.Subjects.Settings;

namespace servicebus_cli;

public class Program
{
    public static async Task Main(string[] args)
    {
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IServiceBusCli, ServiceBusCli>()
            // Deadletter Subject + Actions
            .AddSingleton<IDeadletter, Deadletter>()
            .AddSingleton<IDeadletterActions, DeadletterActions>()
            // Settings Subject + Actions
            .AddSingleton<ISettings, Settings>()
            .AddSingleton<ISettingsActions, SettingsActions>()
            // Queue Subject + Actions
            .AddSingleton<IQueue, Queue>()
            .AddSingleton<IQueueActions, QueueActions>()
            // Help Subject + Actions
            .AddSingleton<IHelp, Help>()
            // Services
            .AddSingleton<IServiceBusService, ServiceBusService>()
            .AddSingleton<IFileService, FileService>()
            .AddSingleton<IUserSettingsService, UserSettingsService>()
            // Repositories
            .AddSingleton<IServiceBusRepository, ServiceBusRepository>()
            .BuildServiceProvider();

        var serviceBusCli = serviceProvider.GetService<IServiceBusCli>();
        await serviceBusCli.Run(args);
    }
}