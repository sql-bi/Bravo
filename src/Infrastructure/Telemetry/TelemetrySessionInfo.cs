namespace Sqlbi.Bravo.Infrastructure.Telemetry;

using Sqlbi.Bravo.Infrastructure.Security;

internal static class TelemetrySessionInfo
{
    /// <summary>See <see cref="Microsoft.ApplicationInsights.Extensibility.Implementation.Endpoints.Constants"/></summary>
    public static Uri DefaultIngestionEndpoint { get; } = new Uri("https://dc.services.visualstudio.com/", UriKind.Absolute);
    public static string ConnectionString { get; } = "InstrumentationKey=47a8970c-6293-408a-9cce-5b7b311574d3";
    public static string ComponentVersion { get; } = AppEnvironment.ApplicationProductVersion;
    public static string DeviceOperatingSystem { get; } = Environment.OSVersion.ToString();
    public static string SessionId { get; } = Guid.NewGuid().ToString();
    public static string UserId { get; } = $"{Environment.MachineName}\\{Environment.UserName}".ToSHA256Hash();
    public static IReadOnlyDictionary<string, string> GlobalProperties { get; } = new Dictionary<string, string>
    {
        { "ProductName", AppEnvironment.ApplicationName },
        { "Version", AppEnvironment.ApplicationProductVersion },
        { "Build", AppEnvironment.ApplicationFileVersion },
        { "PublishMode", AppEnvironment.PublishMode.ToString() },
        { "InstallScope", AppEnvironment.DeploymentMode.ToString() },
        { "WebView2Version", AppEnvironment.WebView2VersionInfo ?? string.Empty },
    };

    public static bool IsTelemetryUri(string uriString)
    {
        var requestedUri = new Uri(uriString);
        var telemetryUri = DefaultIngestionEndpoint;

        // Compare only scheme and host/port
        var result = Uri.Compare(
            requestedUri,
            telemetryUri,
            partsToCompare: UriComponents.Scheme | UriComponents.HostAndPort,
            UriFormat.Unescaped,
            StringComparison.OrdinalIgnoreCase);

        return result == 0;
    }
}
