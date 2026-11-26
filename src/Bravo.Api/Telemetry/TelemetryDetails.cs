using System.Collections.Generic;
using Bravo.Core.Telemetry;

namespace Bravo.Api.Telemetry;

// Public, as every type that travels: the validation and the descriptions of the document are generated
// for public types only.
/// <summary>
/// What the page needs to send its telemetry, with the values of the telemetry of the host. The page sends it
/// only when the user or a policy turned it on. The values do not change while the application runs.
/// </summary>
public sealed record TelemetryDetails
{
    /// <summary>
    /// The connection string of Application Insights, with no slash at the end of its ingestion endpoint. Absent
    /// for a build that sends nothing, such as a development build started without an endpoint.
    /// </summary>
    public string? ConnectionString { get; init; }

    /// <summary>
    /// The identifier of the user on the computer, which tells neither name.
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// The identifier of the run of the application, the same for the host and the page.
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// The version of the application as a user reads it, such as <c>1.1.0</c> or <c>1.1.0-beta.1</c>.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// The properties that every item carries, by their names.
    /// </summary>
    public required IReadOnlyDictionary<string, string> Properties { get; init; }

    internal static TelemetryDetails FromPageTelemetry(PageTelemetry telemetry)
    {
        return new TelemetryDetails
        {
            ConnectionString = telemetry.ConnectionString,
            UserId = telemetry.UserId,
            SessionId = telemetry.SessionId,
            Version = telemetry.Version,
            Properties = telemetry.Properties,
        };
    }
}
