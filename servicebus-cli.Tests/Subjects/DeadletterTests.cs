
namespace servicebus_cli.Tests.Subjects;

public class DeadletterTests
{
    private Mock<IHelp> _help = new Mock<IHelp>();
    private Deadletter _deadletter;

    [SetUp]
    public void Setup()
    {
        _deadletter = new Deadletter(_help.Object);
    }

[Test]
    public async Task Run_WhenProvidedWithEmptyArgs_DoesNotCrashAsync()
    {
        //Arrange
        var emptyArgs = new string[0];

        //Act
        await _deadletter.Run(emptyArgs);

        //Assert
        Assert.Pass();
    }
}