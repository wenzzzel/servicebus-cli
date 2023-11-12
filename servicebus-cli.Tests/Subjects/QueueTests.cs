using servicebus_cli.Repositories;

namespace servicebus_cli.Tests.Subjects;

public class QueueTests
{
    private Mock<IHelp> _help;
    private Mock<IServiceBusRepostitory> _serviceBusRespository;
    private Queue _queue;

    [SetUp]
    public void Setup()
    {
        _help = new Mock<IHelp>();
        _serviceBusRespository = new Mock<IServiceBusRepostitory>();
        _queue = new Queue(_help.Object, _serviceBusRespository.Object);
    }

    [Test]
    public async Task Run_WhenCalledWithNoActions_CallsHelpOnce()
    {
        //Arrange
        var myArgs = Array.Empty<string>();

        //Act
        await _queue.Run(myArgs);

        //Assert
        _help.Verify(x => x.Run(), Times.Once);
    }

    [TestCase("This is an invalid action", "", "valid filter")]     // action invalid, namespace invalid, filter valid
    [TestCase("list", "", "valid filter")]                          // action valid,   namespace invalid, filter valid
    [TestCase("show", "", "valid filter")]                          // action valid,   namespace invalid, filter valid
    [TestCase("This is an invalid action", "valid namespace", "")]  // action invalid, namespace valid,   filter valid
    public async Task Run_WhenCalledWithInvalidArguments_CallsHelpOnce(
        string action, 
        string fullyQualifiedNamespace, 
        string filter)
    {
        //Arrange
        var myArgs = new string[] { action, fullyQualifiedNamespace, filter };

        //Act
        await _queue.Run(myArgs);

        //Assert
        _help.Verify(x => x.Run(), Times.Once);
        _serviceBusRespository.Verify(x => x.ListQueues(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _serviceBusRespository.Verify(x => x.ShowQueue(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Run_WhenCalledWithTooManyArguments_CallsHelpOnce()
    {
        //Arrange
        var myArgs = new string[] { "this", "is", "definitively", "too", "many", "arguments", "I", "would", "say" };

        //Act
        await _queue.Run(myArgs);

        //Assert
        _help.Verify(x => x.Run(), Times.Once);
        _serviceBusRespository.Verify(x => x.ListQueues(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _serviceBusRespository.Verify(x => x.ShowQueue(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestCase("list")]
    [TestCase("show")] // When more valid actions appear, add them here
    public async Task Run_WhenCalledWithValidAction_DoesNotCallHelp(string action)
    {
        //Arrange
        var myArgs = new string[] { action, "fullyQualifiedNamespace" };

        //Act
        await _queue.Run(myArgs);

        //Assert
        _help.Verify(x => x.Run(), Times.Never);
    }

    [Test]
    public async Task Run_WhenCalledWithListAction_CallsListQueuesOnce()
    {
        //Arrange
        var myArgs = new string[] { "list", "fullyQualifiedNamespace" };

        //Act
        await _queue.Run(myArgs);

        //Assert
        _serviceBusRespository.Verify(x => x.ListQueues(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task Run_WhenCalledWithShowAction_CallsShowQueueOnce()
    {
        //Arrange
        var myArgs = new string[] { "show", "fullyQualifiedNamespace" };

        //Act
        await _queue.Run(myArgs);

        //Assert
        _serviceBusRespository.Verify(x => x.ShowQueue(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

}