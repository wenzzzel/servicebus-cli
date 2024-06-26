using servicebus_cli.Services;

namespace servicebus_cli.Tests.Subjects;

public class DeadletterTests
{
    private Mock<IHelp> _help;
    private Mock<IServiceBusService> _serviceBusRespository;
    private Deadletter _deadletter;

    [SetUp]
    public void Setup()
    {
        _help = new Mock<IHelp>();
        _serviceBusRespository = new Mock<IServiceBusService>();
        _deadletter = new Deadletter(_help.Object, _serviceBusRespository.Object);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(10)]
    [TestCase(50)]
    [TestCase(1000)]
    public async Task Run_RegardlessOfNumberOfArguments_GivenThatArgument0IsResend_DoesNotCrashAsync(int argumentCount)
    {
        //Arrange
        var arguments = new List<string>();
        for (int i = 0; i < argumentCount; i++)
        {
            if (i == 0)
                arguments.Add("resend");
            else
                arguments.Add("Random argument");
        }

        var args = arguments.ToArray();

        //Act
        await _deadletter.Run(args);

        //Assert
        Assert.Pass();
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(10)]
    [TestCase(50)]
    [TestCase(1000)]
    public async Task Run_RegardlessOfNumberOfArguments_GivenThatArgument0IsPurge_DoesNotCrashAsync(int argumentCount)
    {
        //Arrange
        var arguments = new List<string>();
        for (int i = 0; i < argumentCount; i++)
        {
            if (i == 0)
                arguments.Add("purge");
            else
                arguments.Add("Random argument");
        }

        var args = arguments.ToArray();

        //Act
        await _deadletter.Run(args);

        //Assert
        Assert.Pass();
    }

    [TestCase("resend", "<FullyQualifiedNamespace>", "<EnitityPath>")]
    [TestCase("resend", "<FullyQualifiedNamespace>", "<EnitityPath>", "<UseSessions>")]
    public async Task Run_WhenValidArgumentsAreProvided_GivenThatArgument0IsResend_DoesNotCallHelp(string arg1, string arg2, string arg3, string arg4 = null)
    {
        //Arrange
        var args = new string[] { arg1, arg2, arg3, arg4 };

        //Act
        await _deadletter.Run(args);

        //Assert
        _help.Verify(x => x.Run(), Times.Never());
        _serviceBusRespository.Verify(x => x.ResendDeadletterMessage(
                                        It.IsAny<string>(), 
                                        It.IsAny<string>(), 
                                        It.IsAny<string>()
                                    ), Times.Once());
    }

    [TestCase("purge", "<FullyQualifiedNamespace>", "<EnitityPath>")]
    public async Task Run_WhenValidArgumentsAreProvided_GivenThatArgument0IsPurge_DoesNotCallHelp(string arg1, string arg2, string arg3)
    {
        //Arrange
        var args = new string[] { arg1, arg2, arg3 };

        //Act
        await _deadletter.Run(args);

        //Assert
        _help.Verify(x => x.Run(), Times.Never());
        _serviceBusRespository.Verify(x => x.PurgeDeadletterQueue(
                                        It.IsAny<string>(), 
                                        It.IsAny<string>()
                                    ), Times.Once());
    }

    [TestCase("notResend", "<FullyQualifiedNamespace>", "<EnitityPath>")]
    [TestCase("notResend", "<FullyQualifiedNamespace>", "<EnitityPath>", "<UseSessions>")]
    public async Task Run_WhenValidArgumentsAreProvided_GivenThatArgument0IsNotResend_CallsHelpOnce(string arg1, string arg2, string arg3, string arg4 = null)
    {
        //Arrange
        var args = new string[] { arg1, arg2, arg3, arg4 };

        //Act
        await _deadletter.Run(args);

        //Assert
        _help.Verify(x => x.Run(), Times.Once());

        _serviceBusRespository.Verify(x => x.ResendDeadletterMessage(
                                                It.IsAny<string>(), 
                                                It.IsAny<string>(), 
                                                It.IsAny<string>()
                                            ), Times.Never());
    }
}