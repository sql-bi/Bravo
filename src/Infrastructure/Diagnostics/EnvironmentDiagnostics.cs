using System;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sqlbi.Bravo.Infrastructure.Telemetry;

namespace Sqlbi.Bravo.Infrastructure.Diagnostics;

internal sealed record EnvironmentDiagnostics(
    [property: JsonPropertyName("timestampUtc")] string TimestampUtc,
    [property: JsonPropertyName("timestampLocal")] string TimestampLocal,
    [property: JsonPropertyName("application")] ApplicationDiagnostics Application,
    [property: JsonPropertyName("process")] ProcessDiagnostics Process,
    [property: JsonPropertyName("os")] OperatingSystemDiagnostics OS,
    [property: JsonPropertyName("runtime")] RuntimeDiagnostics Runtime,
    [property: JsonPropertyName("components")] ComponentDiagnostics Components)
{
    private static readonly JsonSerializerOptions? s_options = new()
    {
        WriteIndented = true
    };

    public string ToJsonString()
        => JsonSerializer.Serialize(this, s_options);
}

internal sealed record ApplicationDiagnostics(
    [property: JsonPropertyName("sessionId")] string SessionId,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("publishMode")] string PublishMode,
    [property: JsonPropertyName("deploymentMode")] string DeploymentMode,
    [property: JsonPropertyName("dataPath")] string DataPath
);

internal sealed record ProcessDiagnostics(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("architecture")] string Architecture,
    [property: JsonPropertyName("processorCount")] string ProcessorCount
);

public sealed record OperatingSystemDiagnostics(
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("architecture")] string Architecture
);

internal sealed record RuntimeDiagnostics(
    [property: JsonPropertyName("identifier")] string Identifier,
    [property: JsonPropertyName("description")] string Description
);

internal sealed record ComponentDiagnostics(
    [property: JsonPropertyName("webView2Version")] string WebView2Version
);

internal static class EnvironmentDiagnosticsCollector
{
    public static EnvironmentDiagnostics Collect()
    {
        return new EnvironmentDiagnostics(
            TimestampUtc: DateTimeOffset.UtcNow.ToString("O"),
            TimestampLocal: DateTime.Now.ToString("O"),
            Application: new(
                SessionId: SafeRead(() => TelemetrySessionInfo.SessionId),
                Version: SafeRead(() => AppVersion.InformationalVersion),
                PublishMode: SafeRead(() => AppEnvironment.PublishMode.ToString()),
                DeploymentMode: SafeRead(() => AppEnvironment.DeploymentMode.ToString()),
                DataPath: SafeRead(() => AppEnvironment.ApplicationDataPath)
            ),
            Process: new(
                Id: SafeRead(() => Environment.ProcessId.ToString()),
                Path: SafeRead(() => Environment.ProcessPath),
                Architecture: SafeRead(() => RuntimeInformation.ProcessArchitecture.ToString()),
                ProcessorCount: SafeRead(() => Environment.ProcessorCount.ToString())
            ),
            OS: new(
                Version: SafeRead(() => Environment.OSVersion.ToString()),
                Description: SafeRead(() => RuntimeInformation.OSDescription),
                Architecture: SafeRead(() => RuntimeInformation.OSArchitecture.ToString())
            ),
            Runtime: new(
                Identifier: SafeRead(() => RuntimeInformation.RuntimeIdentifier),
                Description: SafeRead(() => RuntimeInformation.FrameworkDescription)
            ),
            Components: new(
                WebView2Version: SafeRead(() => AppEnvironment.WebView2VersionInfo)
            )
        );
    }

    private static string SafeRead(Func<string?> read, string fallback = "n/a")
    {
        try
        {
            return read() ?? fallback;
        }
        catch (Exception ex)
        {
            return $"{fallback} ({ex.GetType().Name})";
        }
    }
}
