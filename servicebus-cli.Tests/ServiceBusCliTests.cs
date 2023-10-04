
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
    public async Task Run_RunsWithoutCrashing()
    {
        //Arrange
        var args = new string[0];

        //Act
        await cli.Run(args);

        //Assert
        Assert.Pass();
    }
}
