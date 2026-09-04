using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Sqlbi.Bravo.Infrastructure.Telemetry;

namespace Sqlbi.Bravo.Infrastructure.Diagnostics;

/// <summary>
/// Provides diagnostic information about the environment in which the application is running.
/// </summary>
internal sealed class EnvironmentInfo
{
    private readonly IReadOnlyList<KeyValuePair<string, string>> _entries;

    /// <summary>
    /// Collects the environment information and returns an instance of <see cref="EnvironmentInfo"/>.
    /// </summary>
    public static EnvironmentInfo Collect()
    {
        var entries = new KeyValuePair<string, string>[]
        {
            new("TimestampUtc", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)),
            new("TimestampLocal", DateTime.Now.ToString("O", CultureInfo.InvariantCulture)),
            // Application
            new("ApplicationSessionId", SafeRead(() => TelemetrySessionInfo.SessionId)),
            new("ApplicationVersion", SafeRead(() => AppVersion.InformationalVersion)),
            new("ApplicationPublishMode", SafeRead(() => AppEnvironment.PublishMode.ToString())),
            new("ApplicationDeploymentMode", SafeRead(() => AppEnvironment.DeploymentMode.ToString())),
            new("ApplicationDataPath", SafeRead(() => AppEnvironment.ApplicationDataPath)),
            // Process
            new("ProcessId", SafeRead(() => Environment.ProcessId.ToString(CultureInfo.InvariantCulture))),
            new("ProcessPath", SafeRead(() => Environment.ProcessPath)),
            new("ProcessSessionId", SafeRead(() => AppEnvironment.SessionId.ToString(CultureInfo.InvariantCulture))),
            new("ProcessArchitecture", SafeRead(() => RuntimeInformation.ProcessArchitecture.ToString())),
            new("ProcessProcessorCount", SafeRead(() => Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture))),
            // Machine
            new("OSDescription", SafeRead(() => RuntimeInformation.OSDescription)),
            new("OSArchitecture", SafeRead(() => RuntimeInformation.OSArchitecture.ToString())),
            // Runtime
            new("RuntimeDescription", SafeRead(() => RuntimeInformation.FrameworkDescription)),
            new("RuntimeIdentifier", SafeRead(() => RuntimeInformation.RuntimeIdentifier)),
            // Components
            new("WebView2Version", SafeRead(() => AppEnvironment.WebView2VersionInfo)),
        };

        return new EnvironmentInfo(entries);
    }

    private EnvironmentInfo(IReadOnlyList<KeyValuePair<string, string>> entries)
    {
        _entries = entries;
    }

    /// <summary>
    /// Returns the environment information as a text block.
    /// </summary>
    public string ToText()
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Environment Information");
        builder.AppendLine();

        foreach (var (name, value) in _entries)
            builder.Append($"- {name}: ").AppendLine(value);

        return builder.ToString();
    }

    /// <summary>
    /// Returns the environment information as dictionary.
    /// </summary>
    public Dictionary<string, string> ToDictionary()
    {
        return _entries.ToDictionary((entry) => entry.Key, (entry) => entry.Value);
    }

    internal static string SafeRead(Func<string?> read, string fallback = "n/a")
    {
        try
        {
            return read() ?? fallback;
        }
        catch (Exception ex)
        {
            return $"unavailable ({ex.GetType().Name})";
        }
    }
}
