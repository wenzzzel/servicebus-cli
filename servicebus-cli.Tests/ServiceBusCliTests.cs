
namespace servicebus_cli.Tests;

public class ServiceBusCliTests
{
    private ServiceBusCli cli;
    private readonly Mock<IDeadletter> deadletterMock = new Mock<IDeadletter>();
    private readonly Mock<IHelp> helpMock = new Mock<IHelp>();

    [SetUp]
    public void Setup()
    {
        cli = new ServiceBusCli(deadletterMock.Object, helpMock.Object);
    }

    [Test]
    public async Task Run_WhenCalledWithZeroArguments_HelpIsCalledOnce()
    {
        //Arrange
        var args = Array.Empty<string>();

        //Act
        await cli.Run(args);

        //Assert
        helpMock.Verify(x => x.Run(), Times.Once());
    }

    [Test]
    public async Task Run_WhenCalledWithBadArgument_HelpIsCalledOnce()
    {
        //Arrange
        var args = new string[] { "This_Argument_Does_Not_Make_Any_Sense" };

        //Act
        await cli.Run(args);

        //Assert
        helpMock.Verify(x => x.Run(), Times.Once());
    }

    [Test]
    public async Task Run_WhenCalledWithDeadletterArgument_DeadletterIsCalledOnce()
    {
        //Arrange
        var args = new string[] { "deadletter" };
        var expectedArgument = args.Skip(1).ToArray();

        //Act
        await cli.Run(args);

        //Assert
        deadletterMock.Verify(x => x.Run(expectedArgument), Times.Once());
        helpMock.Verify(x => x.Run(), Times.Never());
    }
}
