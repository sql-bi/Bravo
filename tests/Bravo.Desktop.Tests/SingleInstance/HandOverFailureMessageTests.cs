using System;
using System.Linq;
using Bravo.Desktop.SingleInstance;
using Bravo.Platform.Windows.SingleInstance;
using Xunit;

namespace Bravo.Desktop.Tests.SingleInstance;

public sealed class HandOverFailureMessageTests
{
    [Fact]
    public void For_EveryFailure_ReturnsAMessageOfItsOwn()
    {
        var failures = Enum.GetValues<HandOverFailure>();

        var messages = failures.Select(HandOverFailureMessage.For).ToList();

        Assert.All(messages, message => Assert.Contains("is already running", message));
        Assert.Equal(failures.Length, messages.Distinct().Count());
    }

    [Fact]
    public void For_ValueThatIsNotAFailure_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => HandOverFailureMessage.For((HandOverFailure)(-1)));
    }
}
