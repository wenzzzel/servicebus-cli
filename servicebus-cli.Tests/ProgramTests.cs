namespace servicebus_cli.Tests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Main_RunsWithoutCrashing()
    {
        //Arrange
        var args = new string[0];
        
        //Act
        Program.Main(args);
        
        //Assert
        Assert.Pass();
    }
}