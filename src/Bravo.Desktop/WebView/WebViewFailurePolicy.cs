using System;
using System.Collections.Generic;

namespace Bravo.Desktop.WebView;

/// <summary>
/// The rules for the failures of the webview: what is done for each one, and how many times the application
/// recovers before it gives up. It knows nothing of the engine.
/// </summary>
internal sealed class WebViewFailurePolicy(WebViewFailurePolicyOptions options, TimeProvider timeProvider)
{
    private readonly WebViewFailurePolicyOptions _options = options;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly Queue<long> _recoveries = new();

    /// <summary>
    /// Decides what to do for a failure that has just happened. A decision to recover counts toward the limit.
    /// </summary>
    public WebViewDecision Decide(WebViewFailure failure)
    {
        return failure switch
        {
            WebViewFailure.ContentMissing => WebViewDecision.End(ExitCode.UserInterfaceMissing),
            WebViewFailure.RuntimeMissing => WebViewDecision.End(ExitCode.WebViewRuntimeMissing),
            WebViewFailure.StartFailed => WebViewDecision.End(ExitCode.WebViewUnavailable),
            WebViewFailure.RenderProcessExited => Recover(WebViewRecovery.Reload),
            WebViewFailure.BrowserProcessExited => Recover(WebViewRecovery.Recreate),
            WebViewFailure.Transient => WebViewDecision.Continue(WebViewRecovery.None),
            _ => throw new ArgumentOutOfRangeException(nameof(failure), failure, message: null),
        };
    }

    // A page that fails as soon as it is loaded would be loaded again without end: the recoveries of the
    // last period are counted, of either kind, and one too many ends the application.
    private WebViewDecision Recover(WebViewRecovery recovery)
    {
        var now = _timeProvider.GetTimestamp();
        while (_recoveries.Count > 0 && _timeProvider.GetElapsedTime(_recoveries.Peek(), now) > _options.Period)
        {
            _ = _recoveries.Dequeue();
        }

        if (_recoveries.Count >= _options.MaxRecoveries)
        {
            return WebViewDecision.End(ExitCode.WebViewFailed);
        }

        _recoveries.Enqueue(now);
        return WebViewDecision.Continue(recovery);
    }
}
