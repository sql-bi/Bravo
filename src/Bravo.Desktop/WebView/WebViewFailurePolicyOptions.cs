using System;

namespace Bravo.Desktop.WebView;

/// <summary>
/// The limits of <see cref="WebViewFailurePolicy"/>.
/// </summary>
internal sealed record WebViewFailurePolicyOptions
{
    /// <summary>
    /// How many times, within <see cref="Period"/>, the page is loaded again or the control is created again.
    /// </summary>
    public int MaxRecoveries { get; init; } = 3;

    /// <summary>
    /// The time after which a recovery no longer counts. Long enough for a page that fails at every load on a
    /// slow machine to reach the limit.
    /// </summary>
    public TimeSpan Period { get; init; } = TimeSpan.FromMinutes(5);
}
