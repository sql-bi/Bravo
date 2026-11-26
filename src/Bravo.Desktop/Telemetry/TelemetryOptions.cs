using System;
using System.IO;

namespace Bravo.Desktop.Telemetry;

/// <summary>
/// Where the telemetry of the build goes, and the folder where it keeps what it could not send yet.
/// </summary>
internal sealed record TelemetryOptions
{
    // The resource of Application Insights that 1.0.x sends to: the telemetry of every version reaches the
    // same place. Only a release build has it.
    internal const string ReleaseConnectionString = "InstrumentationKey=47a8970c-6293-408a-9cce-5b7b311574d3";

    // No resource has it: a development build sends only to the endpoint that its command line names.
    internal const string DevelopmentInstrumentationKey = "00000000-0000-0000-0000-000000000000";

    private const string StorageFolderName = "Telemetry";

    // Where the SDKs send when the connection string names no endpoint, as the one of 1.0.x.
    private static readonly Uri s_defaultIngestionEndpoint = new("https://dc.services.visualstudio.com/");

    /// <summary>
    /// The connection string of Application Insights, or null for a build that sends nothing.
    /// </summary>
    public string? ConnectionString { get; init; }

    /// <summary>
    /// The server that the telemetry goes to, or null for a build that sends nothing. The page calls no other
    /// server outside the machine.
    /// </summary>
    public Uri? IngestionEndpoint => GetIngestionEndpoint(ConnectionString);

    /// <summary>
    /// The folder of the data folder where the telemetry waits until it is sent.
    /// </summary>
    public required string StorageFolder { get; init; }

    /// <param name="identity">The build that was started.</param>
    /// <param name="dataFolder">The data folder of the build.</param>
    /// <param name="developmentEndpoint">
    /// The origin of the endpoint that the command line of a development build named, such as the one of a
    /// test on the loopback address.
    /// </param>
    public static TelemetryOptions For(BuildIdentity identity, DataFolder dataFolder, string? developmentEndpoint)
    {
        var connectionString = identity.IsDevelopmentBuild
            ? developmentEndpoint is null
                ? null
                : $"InstrumentationKey={DevelopmentInstrumentationKey};IngestionEndpoint={developmentEndpoint}"
            : ReleaseConnectionString;

        return new TelemetryOptions
        {
            ConnectionString = connectionString,
            StorageFolder = Path.Combine(dataFolder.Path, StorageFolderName),
        };
    }

    private static Uri? GetIngestionEndpoint(string? connectionString)
    {
        if (connectionString is null)
        {
            return null;
        }

        foreach (var field in connectionString.Split(';'))
        {
            var pair = field.Split('=', 2);
            if (pair.Length == 2 && pair[0].Trim().Equals("IngestionEndpoint", StringComparison.OrdinalIgnoreCase))
            {
                return new Uri(pair[1].Trim());
            }
        }

        return s_defaultIngestionEndpoint;
    }
}
