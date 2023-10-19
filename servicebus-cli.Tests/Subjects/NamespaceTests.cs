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

    [TestCase("This is an invalid action", "")]                 // both invalid
    [TestCase("list", "")]                                      // action valid, namespace invalid
    [TestCase("This is an invalid action", "valid namespace")]  // action invalid, namespace valid
    public async Task Run_WhenCalledWithInvalidArguments_CallsHelpOnce(string action, string fullyQualifiedNamespace)
    {
        //Arrange
        var myArgs = new string[] { action, fullyQualifiedNamespace };

        //Act
        await _namespace.Run(myArgs);

        //Assert
        _help.Verify(x => x.Run(), Times.Once);
    }

    [TestCase("list")] // When more valid actions appear, add them here
    public async Task Run_WhenCalledWithValidAction_DoesNotCallHelp(string action)
    {
        //Arrange
        var myArgs = new string[] { action, "fullyQualifiedNamespace" };

        //Act
        await _namespace.Run(myArgs);

        //Assert
        _help.Verify(x => x.Run(), Times.Never);
    }

    [Test]
    public async Task Run_WhenCalledWithListAction_CallsListQueuesOnce()
    {
        //Arrange
        var myArgs = new string[] { "list", "fullyQualifiedNamespace" };

        //Act
        await _namespace.Run(myArgs);

        //Assert
        _serviceBusRespository.Verify(x => x.ListQueues(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

}