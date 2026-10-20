using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Sqlbi.Bravo.Infrastructure.Telemetry;

namespace Sqlbi.Bravo.Infrastructure.Diagnostics;

/// <summary>
/// Handles unhandled exceptions in the application, providing a mechanism
/// to report and display error information to the user.
/// </summary>
internal sealed partial class UnhandledExceptionHandler(IErrorReportDialog dialog)
{
    private readonly IErrorReportDialog _dialog = dialog;
    private volatile ReportingServices _services = new(NullLogger.Instance, Telemetry: null);

    public void AttachServices(ILoggerFactory loggerFactory, ITelemetryService telemetry)
    {
        _services = new ReportingServices(loggerFactory.CreateLogger<UnhandledExceptionHandler>(), telemetry);
    }

    public void Handle(Exception exception, ExceptionOrigin origin)
    {
        var services = _services;

        Report(services, exception, origin);

        // Do not show UI for unobserved task exceptions (finalizer threads are runtime-critical).
        // The process keeps running, so the channel sends the telemetry on its own flush interval.
        if (origin == ExceptionOrigin.UnobservedTask)
            return;

        // Prevent multiple handling of the same exception
        if (!TryMarkHandled(exception))
            return;

        var report = ErrorReport.Create(exception);
        _ = report.TrySave();
        ShowDialog(services, report);

        _ = services.Telemetry?.TryFlushBeforeShutdown(10_000);
    }

    private void ShowDialog(ReportingServices services, ErrorReport report)
    {
        try
        {
            _dialog.Show(report, services.Telemetry);
        }
        catch (Exception ex)
        {
            // The report is already saved; a failing dialog must not turn the crash into a FailFast.
            try
            {
                LogDialogFailed(services.Logger, ex);
            }
            catch { /* Ignore failures */ }
        }
    }

    private static void Report(ReportingServices services, Exception exception, ExceptionOrigin origin)
    {
        try
        {
            LogUnhandledException(services.Logger, exception, origin);
        }
        catch { /* Ignore failures */ }

        try
        {
            services.Telemetry?.TrackException(exception, properties: new Dictionary<string, string>
            {
                ["ExceptionOrigin"] = origin.ToString(),
                ["ExceptionKind"] = "Unhandled"
            });
        }
        catch { /* Ignore failures */ }
    }

    private static bool TryMarkHandled(Exception exception)
    {
        const string HandledMarker = "Bravo.UnhandledExceptionHandler.Handled";

        try
        {
            if (exception.Data.Contains(HandledMarker))
                return false;

            exception.Data[HandledMarker] = true;
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            // Data is read-only or rejects the key: report the exception anyway
            return true;
        }
    }

    private sealed record ReportingServices(ILogger Logger, ITelemetryService? Telemetry);
}
