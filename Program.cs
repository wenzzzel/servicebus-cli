using servicebus_cli;

if(args.Length == 0)
    HelpLogic.Main();
else
    switch (args[0])
    {
        case "deadletter":
            DeadletterLogic.Main(args.Skip<string>(1).ToArray());
            break;
        default:
            HelpLogic.Main();
            break;
    }