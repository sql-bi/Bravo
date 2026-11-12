using System;
using Bravo.Desktop.WebView;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Bravo.Desktop.Tests.WebView;

public sealed class WebViewFailurePolicyTests
{
    private readonly WebViewFailurePolicyOptions _options = new() { MaxRecoveries = 3, Period = TimeSpan.FromMinutes(5) };
    private readonly FakeTimeProvider _time = new();
    private readonly WebViewFailurePolicy _policy;

    public WebViewFailurePolicyTests()
    {
        _policy = new WebViewFailurePolicy(_options, _time);
    }

    // The failure travels by name: its type is not public, and a test method is.
    [Theory]
    [InlineData(nameof(WebViewFailure.ContentMissing), ExitCode.UserInterfaceMissing)]
    [InlineData(nameof(WebViewFailure.RuntimeMissing), ExitCode.WebViewRuntimeMissing)]
    [InlineData(nameof(WebViewFailure.StartFailed), ExitCode.WebViewUnavailable)]
    public void Decide_FailureOfTheStart_EndsTheApplicationWithACodeOfItsOwn(string failure, int exitCode)
    {
        var decision = _policy.Decide(Enum.Parse<WebViewFailure>(failure));

        Assert.Equal(WebViewDecision.End(exitCode), decision);
    }

    // The code that 1.0.x ends with when the runtime is not installed.
    [Fact]
    public void Decide_RuntimeMissing_EndsWithTheCodeOf10x()
    {
        var decision = _policy.Decide(WebViewFailure.RuntimeMissing);

        Assert.Equal(1223, decision.ExitCode);
    }

    [Fact]
    public void Decide_RenderProcessExited_ReloadsThePage()
    {
        var decision = _policy.Decide(WebViewFailure.RenderProcessExited);

        Assert.Equal(WebViewDecision.Continue(WebViewRecovery.Reload), decision);
    }

    [Fact]
    public void Decide_BrowserProcessExited_CreatesTheControlAgain()
    {
        var decision = _policy.Decide(WebViewFailure.BrowserProcessExited);

        Assert.Equal(WebViewDecision.Continue(WebViewRecovery.Recreate), decision);
    }

    [Fact]
    public void Decide_OneFailureMoreThanTheLimitWithinThePeriod_EndsTheApplication()
    {
        _ = _policy.Decide(WebViewFailure.RenderProcessExited);
        _ = _policy.Decide(WebViewFailure.RenderProcessExited);
        _ = _policy.Decide(WebViewFailure.RenderProcessExited);

        var decision = _policy.Decide(WebViewFailure.RenderProcessExited);

        Assert.Equal(WebViewDecision.End(ExitCode.WebViewFailed), decision);
    }

    [Fact]
    public void Decide_FailuresOfBothKinds_CountTowardTheSameLimit()
    {
        _ = _policy.Decide(WebViewFailure.RenderProcessExited);
        _ = _policy.Decide(WebViewFailure.BrowserProcessExited);
        _ = _policy.Decide(WebViewFailure.RenderProcessExited);

        var decision = _policy.Decide(WebViewFailure.BrowserProcessExited);

        Assert.Equal(WebViewDecision.End(ExitCode.WebViewFailed), decision);
    }

    [Fact]
    public void Decide_FailureAfterThePeriodOfTheEarlierOnes_RecoversAgain()
    {
        _ = _policy.Decide(WebViewFailure.RenderProcessExited);
        _ = _policy.Decide(WebViewFailure.RenderProcessExited);
        _ = _policy.Decide(WebViewFailure.RenderProcessExited);
        _time.Advance(_options.Period + TimeSpan.FromSeconds(1));

        var decision = _policy.Decide(WebViewFailure.RenderProcessExited);

        Assert.Equal(WebViewDecision.Continue(WebViewRecovery.Reload), decision);
    }

    [Fact]
    public void Decide_FailureWhenOnlyTheOldestRecoveryIsOutOfThePeriod_RecoversOnce()
    {
        _ = _policy.Decide(WebViewFailure.RenderProcessExited);
        _time.Advance(TimeSpan.FromMinutes(3));
        _ = _policy.Decide(WebViewFailure.RenderProcessExited);
        _ = _policy.Decide(WebViewFailure.RenderProcessExited);
        _time.Advance(TimeSpan.FromMinutes(3));

        var first = _policy.Decide(WebViewFailure.RenderProcessExited);
        var second = _policy.Decide(WebViewFailure.RenderProcessExited);

        Assert.Equal(WebViewRecovery.Reload, first.Recovery);
        Assert.Equal(WebViewRecovery.End, second.Recovery);
    }

    [Fact]
    public void Decide_TransientFailure_DoesNothingAndDoesNotCountTowardTheLimit()
    {
        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(WebViewDecision.Continue(WebViewRecovery.None), _policy.Decide(WebViewFailure.Transient));
        }

        var decision = _policy.Decide(WebViewFailure.RenderProcessExited);

        Assert.Equal(WebViewRecovery.Reload, decision.Recovery);
    }
}
