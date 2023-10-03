using servicebus_cli.Subjects;
using System.ComponentModel.Design;

namespace servicebus_cli.Tests.Subjects;

public class DeadletterServiceTests
{
    private Mock<servicebus_cli.Subjects.IHelpService> _helpService = new Mock<servicebus_cli.Subjects.IHelpService>();
    private DeadletterService _deadletterService;

    [SetUp]
    public void Setup()
    {
        _deadletterService = new DeadletterService(_helpService.Object);
    }

[Test]
    public async Task Run_WhenProvidedWithEmptyArgs_DoesNotCrashAsync()
    {
        //Arrange
        var emptyArgs = new string[0];

        //Act
        await _deadletterService.Run(emptyArgs);

        //Assert
        Assert.Pass();
    }
}