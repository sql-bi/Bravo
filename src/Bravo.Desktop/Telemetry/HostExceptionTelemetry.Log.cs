using System;
using Bravo.Core.Telemetry;
using Microsoft.Extensions.Logging;

namespace Bravo.Desktop.Telemetry;

internal sealed partial class HostExceptionTelemetry
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Telemetry is on: {Source}.")]
    private partial void LogOn(TelemetryConsentSource source);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Telemetry is off: {Source}.")]
    private partial void LogOff(TelemetryConsentSource source);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "Telemetry is off: this build sends it nowhere.")]
    private partial void LogNoDestination();

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "Telemetry cannot start: its folder {Folder} cannot be created.")]
    private partial void LogNotStarted(Exception exception, string folder);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Warning,
        Message = "The telemetry that waits in {Folder} cannot be deleted. It is deleted at the next start.")]
    private partial void LogStoreNotDeleted(Exception exception, string folder);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Warning,
        Message = "An exception could not be sent to the telemetry.")]
    private partial void LogNotSent(Exception exception);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Warning,
        Message = "The telemetry that was not sent yet could not be kept for the next start.")]
    private partial void LogNotKept();
}
