
using servicebus_cli.Repositories;

namespace servicebus_cli.Tests.Repositoryies;

public class ServiceBusRepositoryTests
{
    private ServiceBusRepository _repository;

    [SetUp]
    public void Setup()
    {
        _repository = new ServiceBusRepository();
    }

    [TestCase("", "", "")]
    [TestCase("BadParameter", "", "")]
    [TestCase("", "BadParameter", "")]
    public void ResendDeadletterMessage_WhenFQNamespaceAndEntityPathIsNotBothSet_ThrowsArgumentException(string fullyQualifiedNamespace, string entityPath, string useSession)
    {
        //Arrange

        //Act + Assert
        Assert.ThrowsAsync<ArgumentException>(() => _repository.ResendDeadletterMessage(fullyQualifiedNamespace, entityPath, useSession));
    }

    [TestCase("emea-grip-ip-async-sbus-prod.servicebus.windows.net")]
    public async Task ListQueues_TEMPTEST(string fullyQualifiedNamespace)
    {
        //Arrange

        //Act 
        await _repository.ListQueues(fullyQualifiedNamespace);

        //Assert
        Assert.Pass();
    }
}