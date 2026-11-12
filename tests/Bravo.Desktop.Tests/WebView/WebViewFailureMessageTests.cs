using System;
using Bravo.Desktop.WebView;
using Xunit;

namespace Bravo.Desktop.Tests.WebView;

public sealed class WebViewFailureMessageTests
{
    // The installer of the Evergreen runtime, the address that 1.0.x offers in the same case.
    [Fact]
    public void For_RuntimeMissing_OffersTheInstallerOfTheRuntime()
    {
        var message = WebViewFailureMessage.For(WebViewFailure.RuntimeMissing, error: null);

        Assert.Equal("https://go.microsoft.com/fwlink/p/?LinkId=2124703", message.LinkAddress);
    }

    // The failure travels by name: its type is not public, and a test method is.
    [Theory]
    [InlineData(nameof(WebViewFailure.ContentMissing))]
    [InlineData(nameof(WebViewFailure.StartFailed))]
    [InlineData(nameof(WebViewFailure.RenderProcessExited))]
    [InlineData(nameof(WebViewFailure.BrowserProcessExited))]
    public void For_AnyOtherFailureThatEndsTheApplication_HasATextAndNoAddress(string failure)
    {
        var message = WebViewFailureMessage.For(Enum.Parse<WebViewFailure>(failure), error: null);

        Assert.NotEmpty(message.Heading);
        Assert.NotEmpty(message.Text);
        Assert.Null(message.LinkAddress);
    }

    [Fact]
    public void For_StartFailed_TellsWhatTheEngineReported()
    {
        var error = new UnauthorizedAccessException("Access is denied.");

        var message = WebViewFailureMessage.For(WebViewFailure.StartFailed, error);

        Assert.Contains("System.UnauthorizedAccessException: Access is denied.", message.Text);
    }

    [Fact]
    public void For_FailureThatNeverEndsTheApplication_IsNotAMessage()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WebViewFailureMessage.For(WebViewFailure.Transient, error: null));
    }
}
