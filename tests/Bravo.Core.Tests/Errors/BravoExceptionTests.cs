using System;
using Bravo.Core.Errors;
using Xunit;

namespace Bravo.Core.Tests.Errors;

public sealed class BravoExceptionTests
{
    [Fact]
    public void Constructor_CodeAndMessage_KeepsBoth()
    {
        var exception = new BravoException("ModelChanged", "The model was changed by another program.");

        Assert.Equal("ModelChanged", exception.Code);
        Assert.Equal("The model was changed by another program.", exception.Message);
    }

    [Fact]
    public void Constructor_InnerException_KeepsIt()
    {
        var cause = new InvalidOperationException("The cause.");

        var exception = new BravoException("ModelChanged", "The model was changed by another program.", cause);

        Assert.Same(cause, exception.InnerException);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Constructor_CodeThatIsBlank_Throws(string code)
    {
        Assert.Throws<ArgumentException>(() => new BravoException(code, "A message."));
    }
}
