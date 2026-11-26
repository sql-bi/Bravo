using System.Collections.Generic;

namespace Bravo.Core.Telemetry;

/// <summary>
/// What the page needs to send its own telemetry: where it goes, and what its items carry, the same as the items
/// of the host. The values do not change while the application runs.
/// </summary>
public sealed record PageTelemetry
{
    /// <summary>
    /// The connection string of Application Insights, or null for a build that sends nothing.
    /// </summary>
    public string? ConnectionString { get; init; }

    public required string UserId { get; init; }

    public required string SessionId { get; init; }

    /// <summary>
    /// The semantic version of the build.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// The properties of every item.
    /// </summary>
    public required IReadOnlyDictionary<string, string> Properties { get; init; }
}
