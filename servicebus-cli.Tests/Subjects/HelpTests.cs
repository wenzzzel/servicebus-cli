using servicebus_cli.Subjects;

namespace servicebus_cli.Tests.Subjects;

public class HelpTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Run_RunsWithoutCrashing()
    {
        //Arrange
        
        
        //Act
        Help.Run();
        
        //Assert
        Assert.Pass();
    }
}