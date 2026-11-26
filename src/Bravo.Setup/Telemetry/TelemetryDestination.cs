using System;

namespace Bravo.Setup.Telemetry;

/// <summary>
/// The resource of Application Insights that the events go to: its instrumentation key, and the address that takes
/// the items.
/// </summary>
/// <param name="InstrumentationKey">The key of the resource, which every item carries.</param>
/// <param name="TrackAddress">The address that the items are posted to.</param>
internal sealed record TelemetryDestination(string InstrumentationKey, Uri TrackAddress)
{
    // The resource that the application sends to, the one of 1.0.x: a test checks that both are the same. Only the
    // build of the packages sends to it.
    internal const string ReleaseConnectionString = "InstrumentationKey=47a8970c-6293-408a-9cce-5b7b311574d3";

    // Where the SDKs send when the connection string names no endpoint, as the one of 1.0.x.
    private const string DefaultIngestionEndpoint = "https://dc.services.visualstudio.com/";

    /// <summary>
    /// Reads the instrumentation key and the ingestion endpoint of a connection string. The other fields are for
    /// the SDKs, which read more of them.
    /// </summary>
    /// <exception cref="FormatException">
    /// The connection string has no instrumentation key, or an endpoint that is not an absolute address.
    /// </exception>
    public static TelemetryDestination Parse(string connectionString)
    {
        ArgumentNullException.ThrowIfNull(connectionString);

        string? instrumentationKey = null;
        var endpoint = DefaultIngestionEndpoint;
        foreach (var field in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (field.Split('=', 2, StringSplitOptions.TrimEntries) is not [var name, var value] || value.Length == 0)
            {
                continue;
            }

            if (name.Equals("InstrumentationKey", StringComparison.OrdinalIgnoreCase))
            {
                instrumentationKey = value;
            }
            else if (name.Equals("IngestionEndpoint", StringComparison.OrdinalIgnoreCase))
            {
                endpoint = value;
            }
        }

        if (instrumentationKey is null)
        {
            throw new FormatException("The connection string has no instrumentation key.");
        }

        // The path of the SDKs after the endpoint, which is written with its last slash or without it.
        var trackAddress = new Uri(new Uri(endpoint.TrimEnd('/') + "/", UriKind.Absolute), "v2/track");
        return new TelemetryDestination(instrumentationKey, trackAddress);
    }
}
