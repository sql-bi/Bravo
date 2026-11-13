using System;
using Bravo.Core.Errors;
using Xunit;

namespace Bravo.Core.Tests.Errors;

public sealed class BravoExceptionTests
{
    private const string Message = "The model was changed by another program.";

    /// <summary>
    /// Every code that a use case can report.
    /// </summary>
    public static TheoryData<ErrorCode> ErrorCodes => [.. Enum.GetValues<ErrorCode>()];

    // Without data while no use case reports an error: the test runs for the first code that is added.
    [Theory(SkipTestWithoutData = true)]
    [MemberData(nameof(ErrorCodes))]
    public void Constructor_CodeOfTheList_KeepsItWithTheMessageAndTheCause(ErrorCode code)
    {
        var cause = new InvalidOperationException("The cause.");

        var exception = new BravoException(code, Message, cause);

        Assert.Equal(code, exception.Code);
        Assert.Equal(Message, exception.Message);
        Assert.Same(cause, exception.InnerException);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void Constructor_NumberThatIsNoCodeOfTheList_Throws(int number)
    {
        var code = (ErrorCode)number;
        var cause = new InvalidOperationException("The cause.");

        Assert.Throws<ArgumentOutOfRangeException>(() => new BravoException(code, Message));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BravoException(code, Message, cause));
    }
}
