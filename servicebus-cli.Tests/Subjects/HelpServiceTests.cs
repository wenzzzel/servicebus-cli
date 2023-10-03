using servicebus_cli.Subjects;

namespace servicebus_cli.Tests.Subjects;

public class HelpServiceTests
{
    private HelpService _helpService;

    [SetUp]
    public void Setup()
    {
        _helpService = new HelpService();
    }

    [Test]
    public void Run_RunsWithoutCrashing()
    {
        //Arrange


        //Act
        _helpService.Run();

        //Assert
        Assert.Pass();
    }
}