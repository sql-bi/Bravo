using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Bravo.Core.Telemetry;
using Bravo.Core.Updates;

namespace Bravo.Desktop.Telemetry;

/// <summary>
/// What every item of the telemetry of a run carries, with the names and the values of 1.0.x: the queries of
/// its resource read the items of every version alike.
/// </summary>
internal sealed record TelemetryContext
{
    /// <summary>
    /// The hash of the computer and of the user: the same for every run of one user on one computer, and it
    /// tells neither name.
    /// </summary>
    public required string UserId { get; init; }

    public required string SessionId { get; init; }

    /// <summary>
    /// The hash of the name of the computer, in place of the name that the SDK would send.
    /// </summary>
    public required string RoleInstance { get; init; }

    public required string OperatingSystem { get; init; }

    /// <summary>
    /// The semantic version of the build.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// The properties of every item.
    /// </summary>
    public required IReadOnlyDictionary<string, string> Properties { get; init; }

    /// <param name="identity">The build that runs.</param>
    /// <param name="session">The run.</param>
    /// <param name="installation">How the copy that runs was installed.</param>
    /// <param name="webViewVersion">The version of the WebView2 runtime, or null when none is installed.</param>
    public static TelemetryContext Create(
        BuildIdentity identity,
        TelemetrySession session,
        Installation installation,
        string? webViewVersion)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(session);

        return new TelemetryContext
        {
            UserId = Hash($@"{Environment.MachineName}\{Environment.UserName}"),
            SessionId = session.Id,
            RoleInstance = Hash(Environment.MachineName),
            OperatingSystem = Environment.OSVersion.ToString(),
            Version = identity.Version,
            Properties = new Dictionary<string, string>
            {
                ["ProductName"] = Product.ShortName,
                ["Version"] = identity.Version,
                ["Build"] = identity.FileVersion.ToString(),
                ["PublishMode"] = GetPublishMode(identity),
                ["InstallScope"] = GetInstallScope(installation),
                ["WebView2Version"] = webViewVersion ?? string.Empty,
            },
        };
    }

    /// <summary>
    /// What the page carries in its items, the values of the host, with where it sends them.
    /// </summary>
    /// <param name="connectionString">The connection string of the build, or null for a build that sends nothing.</param>
    public PageTelemetry ForPage(string? connectionString)
    {
        return new PageTelemetry
        {
            ConnectionString = connectionString,
            UserId = UserId,
            SessionId = SessionId,
            Version = Version,
            Properties = Properties,
        };
    }

    /// <summary>
    /// The SHA-256 of the text in UTF-8, as lowercase hexadecimal digits: the hash of 1.0.x.
    /// </summary>
    internal static string Hash(string text)
    {
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }

    // The values of 1.0.x, whose development builds sent None.
    private static string GetPublishMode(BuildIdentity identity)
    {
        if (identity.IsDevelopmentBuild)
        {
            return "None";
        }

        return identity.IncludesRuntime ? "SelfContained" : "FrameworkDependent";
    }

    // The values of 1.0.x, which called a copy that no package installed a portable one.
    private static string GetInstallScope(Installation installation)
    {
        return installation switch
        {
            Installation.PerMachine => "PerMachine",
            Installation.PerUser => "PerUser",
            _ => "Portable",
        };
    }
}
