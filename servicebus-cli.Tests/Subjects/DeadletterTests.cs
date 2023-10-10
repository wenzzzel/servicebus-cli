
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

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(10)]
    [TestCase(50)]
    [TestCase(1000)]
    public async Task Run_RegardlessOfNumberOfArguments_GivenThatArgument0IsResend_DoesNotCrashAsync(int argumentCount)
    {
        //Arrange
        var arguments = new List<string>();
        for(int i = 0; i < argumentCount; i++)
        {
            if (i == 0) 
                arguments.Add("resend");
            else 
                arguments.Add("Random argument");
        }

        var args = arguments.ToArray();

        //Act
        await _deadletter.Run(args);

        //Assert
        Assert.Pass();
    }
}