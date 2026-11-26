namespace Bravo.Desktop.Diagnostics;

/// <summary>
/// Whether the reporter sends the crash of its report, and how.
/// </summary>
internal enum CrashReportSubmissionMode
{
    /// <summary>
    /// The build sends no telemetry.
    /// </summary>
    NoTelemetry,

    /// <summary>
    /// An earlier build wrote the report, without the exception as the telemetry sends it.
    /// </summary>
    EarlierFormat,

    /// <summary>
    /// The policies cannot be read, and the application would not start: nothing is sent.
    /// </summary>
    PoliciesUnreadable,

    /// <summary>
    /// A policy turns telemetry off.
    /// </summary>
    Forbidden,

    /// <summary>
    /// A policy or the option of the user turns telemetry on: the reporter sends at once.
    /// </summary>
    Automatic,

    /// <summary>
    /// The option of the user is off, or the settings cannot be read: the reporter sends this report alone if the
    /// user asks.
    /// </summary>
    OnRequest,
}
