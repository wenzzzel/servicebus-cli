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
    private string _fullyQualifiedNamespace = "fullyQualifiedNamespace";
    private string _entityPath = "entityPath";
    private string _useSession = "Y";

    [SetUp]
    public void Setup()
    {
        _serviceBusRespository = new Mock<IServiceBusRepository>();
        _service = new ServiceBusService(_serviceBusRespository.Object);

        var receivedMessages = fixture.Create<IReadOnlyList<ServiceBusReceivedMessage>>();
        var serviceBusReceiver = new Mock<ServiceBusReceiver>();
        serviceBusReceiver.Setup(x => x.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(receivedMessages));
        var serviceBusSender = new Mock<ServiceBusSender>();

        var serviceBusClient = new Mock<ServiceBusClient>();
        serviceBusClient.Setup(x => x.CreateReceiver(It.IsAny<string>(), It.IsAny<ServiceBusReceiverOptions>())).Returns(serviceBusReceiver.Object);
        serviceBusClient.Setup(x => x.CreateSender(It.IsAny<string>())).Returns(serviceBusSender.Object);
        _serviceBusRespository.Setup(x => x.GetServiceBusClient(It.IsAny<string>())).Returns(serviceBusClient.Object);

        var queueRuntimeProperties = ServiceBusModelFactory.QueueRuntimeProperties(_entityPath, deadLetterMessageCount: 100);
        var queueRuntimePropertiesMock = new Mock<Azure.Response<QueueRuntimeProperties>>();
        queueRuntimePropertiesMock.SetupGet(x => x.Value).Returns(queueRuntimeProperties);

        var page = Page<QueueProperties>.FromValues(new List<QueueProperties>
        {
            ServiceBusModelFactory.QueueProperties(_entityPath, new TimeSpan(1, 1, 1), 1, false, true, new TimeSpan(1, 1, 1), new TimeSpan(1, 1, 1), false, new TimeSpan(1, 1, 1), 1, false, new EntityStatus(), "", "", "", false)
        }, continuationToken: null, new Mock<Response>().Object);
        var pages = AsyncPageable<QueueProperties>.FromPages(new[] { page });

        var serviceBusAdministrationClient = new Mock<ServiceBusAdministrationClient>();
        serviceBusAdministrationClient.Setup(x => x.GetQueueRuntimePropertiesAsync(It.IsAny<string>(), default)).Returns(Task.FromResult(queueRuntimePropertiesMock.Object));
        serviceBusAdministrationClient.Setup(x => x.GetQueuesAsync(It.IsAny<CancellationToken>())).Returns(pages);

        _serviceBusRespository.Setup(x => x.GetServiceBusAdministrationClient(It.IsAny<string>())).Returns(serviceBusAdministrationClient.Object);
    }

    [Test]
    public async Task ResendDeadletterMessage_HappyFlow()
    {
        //Arrange

        //Act
        await _service.ResendDeadletterMessage(_fullyQualifiedNamespace, _entityPath, _useSession);

        //Assert
        Assert.Pass();
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

}