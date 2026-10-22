using System;
using Sqlbi.Bravo.Infrastructure.Telemetry;

namespace Sqlbi.Bravo.Infrastructure.Diagnostics.CrashReporting;

/// <summary>
/// The report of a fatal exception, written by the process that crashed and read by the reporter process.
/// </summary>
/// <param name="Timestamp">When the exception reached the handler, in UTC.</param>
/// <param name="Version">The version of Bravo that crashed.</param>
/// <param name="SessionId">The telemetry session of the process that crashed, when it got that far.</param>
/// <param name="TelemetryAllowed">Whether telemetry was allowed when the process crashed: only then can the report be sent.</param>
/// <param name="ExceptionType">The full name of the type of the exception, which telemetry groups crashes by.</param>
/// <param name="ExceptionMessage">The message of the exception, which the dialog shows.</param>
/// <param name="ExceptionText">The exception as its ToString writes it: inner exceptions and stack traces included.</param>
internal sealed record CrashReport(
    DateTimeOffset Timestamp,
    string Version,
    string? SessionId,
    bool TelemetryAllowed,
    string ExceptionType,
    string ExceptionMessage,
    string ExceptionText)
{
    /// <summary>
    /// Creates the report, with the text in <see cref="SensitiveTags"/> redacted and the folders of the user profile
    /// replaced by environment variables: the file leaves the machine when the user attaches it to an issue.
    /// </summary>
    public static CrashReport Create(
        Exception exception,
        string? sessionId,
        bool telemetryAllowed,
        DateTimeOffset timestamp,
        UserProfilePathRedactor redactor)
    {
        var exceptionType = exception.GetType();

        return new CrashReport(
            timestamp,
            AppVersion.InformationalVersion,
            sessionId,
            telemetryAllowed,
            exceptionType.FullName ?? exceptionType.Name,
            Redact(exception.Message),
            Redact(exception.ToString()));

        string Redact(string text) => redactor.Redact(SensitiveTags.Redact(text));
    }
}
