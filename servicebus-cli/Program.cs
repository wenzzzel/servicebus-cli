using servicebus_cli.Subjects;

namespace servicebus_cli;

public class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length == 0)
            Help.Run();
        else
            switch (args[0])
            {
                case "deadletter":
                    await Deadletter.Run(args.Skip(1).ToArray());
                    break;
                default:
                    Help.Run();
                    break;
            }
    }
}