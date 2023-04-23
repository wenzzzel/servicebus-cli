using servicebus_cli.Subjects;

if(args.Length == 0)
    Help.Run();
else
    switch (args[0])
    {
        case "deadletter":
            Deadletter.Run(args.Skip(1).ToArray());
            break;
        default:
            Help.Run();
            break;
    }