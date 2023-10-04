
namespace servicebus_cli.Tests.Subjects;

public class HelpTests
{
    private Help _help;

    [SetUp]
    public void Setup()
    {
        _help = new Help();
    }

    [Test]
    public void Run_RunsWithoutCrashing()
    {
        //Arrange


        //Act
        _help.Run();

        //Assert
        Assert.Pass();
    }
}