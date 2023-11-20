namespace servicebus_cli.Tests;

public class ProgramTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public async Task Main_RunsWithoutCrashing()
    {
        //Arrange
        var args = new string[0];
        
        //Act
        await Program.Main(args);
        
        //Assert
        Assert.Pass();
    }
}