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
}