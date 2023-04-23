using servicebus_cli.Subjects;

namespace servicebus_cli.Tests.Subjects;

public class DeadletterTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Run_WhenProvidedWithEmptyArgs_DoesNotCrash()
    {
        //Arrange
        var emptyArgs = new string[0];
        
        //Act
        Deadletter.Run(emptyArgs);
        
        //Assert
        Assert.Pass();
    }
}