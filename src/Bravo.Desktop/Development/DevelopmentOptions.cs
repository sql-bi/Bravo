using System;

namespace Bravo.Desktop.Development;

/// <summary>
/// What a development build is asked for on its command line: that its user interface can run in a browser,
/// the update feed that it checks, the folder of its data, the key of its policies, the endpoint of its
/// telemetry, and a crash. A release build has none of these: its command line does not know them.
/// </summary>
internal sealed record DevelopmentOptions
{
    /// <summary>
    /// A start that asks for nothing, as every start of a release build is.
    /// </summary>
    public static DevelopmentOptions None { get; } = new();

    /// <summary>
    /// One more origin whose pages may call the API: the one of the development server.
    /// </summary>
    public string? AllowedOrigin { get; init; }

    /// <summary>
    /// The file where the host writes the address and the token of the API once it has started. Whoever can
    /// read the file can call the API.
    /// </summary>
    public string? ConnectionFilePath { get; init; }

    /// <summary>
    /// The update feed that the build checks. A development build has no other: without this one it does not
    /// check.
    /// </summary>
    public Uri? UpdateFeedAddress { get; init; }

    /// <summary>
    /// The folder of the data of the build, in place of the one under the local application data of the user:
    /// a test starts the application with settings and a profile of the webview of its own.
    /// </summary>
    public string? DataFolderPath { get; init; }

    /// <summary>
    /// The key under <c>HKEY_CURRENT_USER</c> where the build reads its policies. A development build has no
    /// other: without this one it reads none, and the policies of the machine do not reach a test.
    /// </summary>
    public string? PolicyKeyName { get; init; }

    /// <summary>
    /// The origin of the endpoint that receives the telemetry of the build, such as the one of a test on the
    /// loopback address. A development build has no other: without this one it sends nothing.
    /// </summary>
    public string? TelemetryEndpoint { get; init; }

    /// <summary>
    /// Where the build throws an exception that nothing catches, or null for nowhere.
    /// </summary>
    public DevelopmentCrash? Crash { get; init; }
}
