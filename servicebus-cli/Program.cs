using Microsoft.Extensions.DependencyInjection;
using servicebus_cli.Subjects;

namespace servicebus_cli;

public class Program
{
    public static async Task Main(string[] args)
    {
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IDeadletterService, DeadletterService>()
            .AddSingleton<IHelpService, HelpService>()
            .BuildServiceProvider();

        var deadletterService = serviceProvider.GetService<IDeadletterService>();
        var helpService = serviceProvider.GetService<IHelpService>();

        if (args.Length == 0)
            helpService.Run();
        else
            switch (args[0])
            {
                case "deadletter":
                    await deadletterService.Run(args.Skip(1).ToArray());
                    break;
                default:
                    helpService.Run();
                    break;
            }
    }
}