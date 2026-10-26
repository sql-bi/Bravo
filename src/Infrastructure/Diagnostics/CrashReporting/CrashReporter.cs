using System;
using System.Threading;
using System.Threading.Tasks;
using Sqlbi.Bravo.Infrastructure.Telemetry;

namespace Sqlbi.Bravo.Infrastructure.Diagnostics.CrashReporting;

/// <summary>
/// Shows the report of a crash, in the reporter process, and sends it to telemetry when the user asks and telemetry
/// was allowed.
/// </summary>
/// <remarks>
/// When the endpoint cannot be reached, the telemetry channel keeps the report on disk and sends it from the next
/// process that starts. Telemetry is created only to send, so the dialog opens fast.
/// </remarks>
internal sealed class CrashReporter(CrashReportStore store, ITelemetryServiceFactory telemetryFactory, ICrashReportDialog dialog)
{
    private static readonly TimeSpan s_sendTimeout = TimeSpan.FromSeconds(10);

    private readonly CrashReportStore _store = store;
    private readonly ITelemetryServiceFactory _telemetryFactory = telemetryFactory;
    private readonly ICrashReportDialog _dialog = dialog;
    private ITelemetryService? _telemetry;
    private Task<bool>? _sending;

    /// <summary>
    /// Shows the report until the user closes the dialog, then waits for a send in progress and prunes the store.
    /// </summary>
    public void Run(string reportPath)
    {
        _ = _store.TryLoad(reportPath, out var report);

        _dialog.Show(CreatePresentation(reportPath, report));

        WaitForSending();
        _store.Prune();
    }

    private CrashReportPresentation CreatePresentation(string reportPath, CrashReport? report)
    {
        if (report is not { TelemetryAllowed: true })
        {
            return new CrashReportPresentation(reportPath, report, Send: null);
        }

        // On the calling thread, the STA thread of the dialog: creating telemetry runs the static constructor of
        // AppEnvironment, whose COM probe of the WebView2 runtime can block on an MTA thread. Sent once: a second call
        // returns the first send.
        Task<bool> Send() => _sending ??= SendAsync(report);

        return new CrashReportPresentation(reportPath, report, Send);
    }

    private async Task<bool> SendAsync(CrashReport report)
    {
        var telemetry = _telemetryFactory.Create(enabled: true);
        _telemetry = telemetry;
        telemetry.TrackException(CrashReportTelemetry.Create(report));

        try
        {
            using var timeout = new CancellationTokenSource(s_sendTimeout);
            return await telemetry.FlushAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    // The dialog can close before the send completes; the process must not end while it is in progress. The telemetry
    // is disposed only then, so that the dialog does not wait for it: Dispose waits, within its own bound, for a
    // transmission still in progress. Its failures are ignored: the dialog is closed, and the reporter reports no crash
    // of its own.
    private void WaitForSending()
    {
        try
        {
            _sending?.Wait(s_sendTimeout);
        }
        catch (AggregateException)
        {
            // The dialog already showed the failure.
        }

        try
        {
            _telemetry?.Dispose();
        }
        catch { /* Ignore failures */ }
    }
}

internal interface ICrashReportDialog
{
    /// <summary>
    /// Shows the report and returns when the user closes the dialog.
    /// </summary>
    void Show(CrashReportPresentation presentation);
}

/// <param name="ReportPath">The report file, which the user can open.</param>
/// <param name="Report">The report, or <see langword="null"/> when the file cannot be read.</param>
/// <param name="Send">
/// Sends the report when the user asks, or <see langword="null"/> when it cannot be sent because telemetry was not
/// allowed or the file cannot be read.
/// </param>
internal sealed record CrashReportPresentation(
    string ReportPath,
    CrashReport? Report,
    Func<Task<bool>>? Send);
