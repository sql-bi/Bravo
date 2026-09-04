using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Infrastructure.Telemetry;

namespace Sqlbi.Bravo.Infrastructure.Diagnostics;

/// <summary>
/// Handles unhandled exceptions in the application, providing a mechanism
/// to report and display error information to the user.
/// </summary>
internal sealed class UnhandledExceptionHandler(IErrorReportDialog dialog, ITelemetryService telemetry, ILoggerFactory loggerFactory)
{
    private readonly IErrorReportDialog _dialog = dialog;
    private readonly ITelemetryService _telemetry = telemetry;
    private readonly ILogger _logger = loggerFactory.CreateLogger<UnhandledExceptionHandler>();

    public void Handle(Exception exception, ExceptionOrigin origin)
    {
        Report(exception, origin);

        // Do not show UI for unobserved task exceptions (finalizer threads are runtime-critical).
        // The process keeps running, so the channel sends the telemetry on its own flush interval.
        if (origin == ExceptionOrigin.UnobservedTask)
            return;

        // Prevent multiple handling of the same exception
        if (!TryMarkHandled(exception))
            return;

        var report = ErrorReport.Create(exception);
        _ = report.TrySave();
        _dialog.Show(report);

        _ = _telemetry.TryFlushBeforeShutdown(10_000);
    }

    private void Report(Exception exception, ExceptionOrigin origin)
    {
        try
        {
            _logger.LogError(exception, "An unhandled exception occurred. Origin: {Origin}", origin);
        }
        catch { /* Ignore failures */ }

        try
        {
            _telemetry.TrackException(exception, properties: new Dictionary<string, string>
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
}
