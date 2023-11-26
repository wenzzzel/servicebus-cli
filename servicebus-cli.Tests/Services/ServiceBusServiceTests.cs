using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using servicebus_cli.Repositories;
using servicebus_cli.Services;
using Azure;

namespace servicebus_cli.Tests.Services;

public class ServiceBusServiceTests
{
    private Fixture fixture = new Fixture();
    private ServiceBusService _service;
    private Mock<IServiceBusRepository> _serviceBusRespository;
    private Mock<ServiceBusReceiver> _serviceBusReceiver;
    private Mock<ServiceBusSender> _serviceBusSender;
    private Mock<ServiceBusClient> _serviceBusClient;
    private Mock<ServiceBusAdministrationClient> _serviceBusAdministrationClient;
    private int _deadLetterMessageCount = 100;
    private string _fullyQualifiedNamespace = "fullyQualifiedNamespace";
    private string _entityPath = "entityPath";
    private string _useSession = "Y";

    [SetUp]
    public void Setup()
    {       
        _serviceBusReceiver = SetupServiceBusReceiverMock();
        _serviceBusSender = SetupServiceBusSenderMock();

        _serviceBusClient = new Mock<ServiceBusClient>();
        _serviceBusClient.Setup(x => x.CreateReceiver(It.IsAny<string>(), It.IsAny<ServiceBusReceiverOptions>())).Returns(_serviceBusReceiver.Object);
        _serviceBusClient.Setup(x => x.CreateSender(It.IsAny<string>())).Returns(_serviceBusSender.Object);

        _serviceBusRespository = new Mock<IServiceBusRepository>();
        _serviceBusRespository.Setup(x => x.GetServiceBusClient(It.IsAny<string>())).Returns(_serviceBusClient.Object);

        var queueRuntimeProperties = SetupQueueRuntimePropertiesMock();
        var queueProperties = SetupQueueProperties();

        _serviceBusAdministrationClient = new Mock<ServiceBusAdministrationClient>();
        _serviceBusAdministrationClient.Setup(x => x.GetQueueRuntimePropertiesAsync(It.IsAny<string>(), default)).Returns(Task.FromResult(queueRuntimeProperties.Object));
        _serviceBusAdministrationClient.Setup(x => x.GetQueuesAsync(It.IsAny<CancellationToken>())).Returns(queueProperties);

        _serviceBusRespository.Setup(x => x.GetServiceBusAdministrationClient(It.IsAny<string>())).Returns(_serviceBusAdministrationClient.Object);

        _service = new ServiceBusService(_serviceBusRespository.Object);
    }

    [Test]
    public async Task ResendDeadletterMessage_WhenHappyFlow_AllDependenciesAreInvokedCorrectly()
    {
        //Arrange

        //Act
        await _service.ResendDeadletterMessage(_fullyQualifiedNamespace, _entityPath, _useSession);

        //Assert
        _serviceBusRespository.Verify(x => x.GetServiceBusClient(It.IsAny<string>()), Times.Once);
        _serviceBusClient.Verify(x => x.CreateSender(It.IsAny<string>()), Times.Once);
        _serviceBusClient.Verify(x => x.CreateReceiver(It.IsAny<string>(), It.IsAny<ServiceBusReceiverOptions>()), Times.Once);
        _serviceBusRespository.Verify(x => x.GetServiceBusAdministrationClient(It.IsAny<string>()), Times.Once);
        _serviceBusAdministrationClient.Verify(x => x.GetQueueRuntimePropertiesAsync(It.IsAny<string>(), (default)), Times.Once);
        _serviceBusReceiver.Verify(x => x.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), (default)), Times.AtLeastOnce);
        _serviceBusSender.Verify(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), (default)), Times.AtLeast(_deadLetterMessageCount));
    }

    [Test]
    public async Task ListQueues_HappyFlow()
    {
        //Arrange

        //Act
        await _service.ListQueues(_fullyQualifiedNamespace);

        //Assert
        Assert.Pass();
    }

    [Test]
    public async Task ShowQueue_HappyFlow()
    {
        //Arrange

        //Act
        await _service.ShowQueue(_fullyQualifiedNamespace, _entityPath);

        //Assert
        Assert.Pass();
    }


    private Mock<ServiceBusReceiver> SetupServiceBusReceiverMock()
    {
        var receivedMessages = fixture.Create<IReadOnlyList<ServiceBusReceivedMessage>>();
        var serviceBusReceiver = new Mock<ServiceBusReceiver>();
        serviceBusReceiver.Setup(x => x.
            ReceiveMessagesAsync(
                It.IsAny<int>(), 
                It.IsAny<TimeSpan>(), 
                It.IsAny<CancellationToken>())
            ).Returns(Task.FromResult(receivedMessages));

        return serviceBusReceiver;
    }

    private Mock<ServiceBusSender> SetupServiceBusSenderMock() => new Mock<ServiceBusSender>();

    private Mock<Response<QueueRuntimeProperties>> SetupQueueRuntimePropertiesMock()
    {
        var queueRuntimeProperties = ServiceBusModelFactory.QueueRuntimeProperties(_entityPath, deadLetterMessageCount: _deadLetterMessageCount);
        var queueRuntimePropertiesMock = new Mock<Response<QueueRuntimeProperties>>();
        queueRuntimePropertiesMock.SetupGet(x => x.Value).Returns(queueRuntimeProperties);

        return queueRuntimePropertiesMock;
    }

    private AsyncPageable<QueueProperties> SetupQueueProperties()
    {
        var queueProperties = ServiceBusModelFactory.QueueProperties(_entityPath, new TimeSpan(1, 1, 1), 1, false, true, new TimeSpan(1, 1, 1), new TimeSpan(1, 1, 1), false, new TimeSpan(1, 1, 1), 1, false, new EntityStatus(), "", "", "", false);
        
        var page = Page<QueueProperties>.FromValues(
            new List<QueueProperties>{queueProperties}, 
            continuationToken: null, 
            new Mock<Response>().Object);
        
        var pages = AsyncPageable<QueueProperties>.FromPages(new[] { page });

        return pages;
    }
}