
namespace servicebus_cli.Tests;

public class ServiceBusCliTests
{
    private ServiceBusCli cli;
    private Mock<IDeadletter> _deadletterMock;
    private Mock<INamespace> _namespaceMock;
    private Mock<IHelp> _helpMock;

    [SetUp]
    public void Setup()
    {
        _deadletterMock = new Mock<IDeadletter>();
        _namespaceMock = new Mock<INamespace>();
        _helpMock = new Mock<IHelp>();
        cli = new ServiceBusCli(_deadletterMock.Object, _helpMock.Object, _namespaceMock.Object);
    }

    [Test]
    public async Task Run_WhenCalledWithZeroArguments_HelpIsCalledOnce()
    {
        //Arrange
        var args = Array.Empty<string>();

        //Act
        await cli.Run(args);

        //Assert
        _helpMock.Verify(x => x.Run(), Times.Once());
    }

    [Test]
    public async Task Run_WhenCalledWithBadArgument_HelpIsCalledOnce()
    {
        //Arrange
        var args = new string[] { "This_Argument_Does_Not_Make_Any_Sense" };

        //Act
        await cli.Run(args);

        //Assert
        _helpMock.Verify(x => x.Run(), Times.Once());
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
        _deadletterMock.Verify(x => x.Run(expectedArgument), Times.Once());
        _helpMock.Verify(x => x.Run(), Times.Never());
    }
}
