using servicebus_cli.Repositories;

namespace servicebus_cli.Tests.Subjects;

public class NamespaceTests
{
    private Mock<IHelp> _help;
    private Mock<IServiceBusRepostitory> _serviceBusRespository;
    private Namespace _namespace;

    [SetUp]
    public void Setup()
    {
        _help = new Mock<IHelp>();
        _serviceBusRespository = new Mock<IServiceBusRepostitory>();
        _namespace = new Namespace(_help.Object, _serviceBusRespository.Object);
    }

    [Test]
    public async Task Run_WhenCalledWithNoActions_CallsHelpOnce()
    {
        //Arrange
        var myArgs = Array.Empty<string>();

        //Act
        await _namespace.Run(myArgs);

        //Assert
        _help.Verify(x => x.Run(), Times.Once);
    }

    [TestCase("This is an invalid action")]
    public async Task Run_WhenCalledWithInvalidValidAction_CallsHelpOnce(string action)
    {
        //Arrange
        var myArgs = new string[] { action };

        //Act
        await _namespace.Run(myArgs);

        //Assert
        _help.Verify(x => x.Run(), Times.Once);
    }

    [TestCase("list")] // <- When more valid actions appear, add them here
    public async Task Run_WhenCalledWithValidAction_DoesNotCallHelp(string action)
    {
        //Arrange
        var myArgs = new string[] { action };

        //Act
        await _namespace.Run(myArgs);

        //Assert
        _help.Verify(x => x.Run(), Times.Never);
    }

}