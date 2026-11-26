using System.Collections.Generic;
using Bravo.Desktop.Telemetry;

namespace Bravo.Desktop.Diagnostics;

/// <summary>
/// The exception of a crash.
/// </summary>
internal sealed record CrashReportError
{
    /// <summary>
    /// The full name of its type.
    /// </summary>
    public required string Type { get; init; }

    public required string Message { get; init; }

    /// <summary>
    /// The lines of its text: its type, its message, its causes and the stacks of all of them.
    /// </summary>
    public required IReadOnlyList<string> Text { get; init; }

    /// <summary>
    /// The exception and the ones that it holds, as the telemetry sends them, or null in a report of format 1.
    /// </summary>
    public IReadOnlyList<ExceptionLink>? Chain { get; init; }
}
