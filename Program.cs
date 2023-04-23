using servicebus_cli.Arguments;

if(args.Length == 0)
    Help.Main();
else
    switch (args[0])
    {
        case "deadletter":
            Deadletter.Main(args.Skip(1).ToArray());
            break;
        default:
            Help.Main();
            break;
    }