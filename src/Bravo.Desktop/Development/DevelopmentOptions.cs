using System;

namespace Bravo.Desktop.Development;

/// <summary>
/// What a development build is asked for on its command line: that its user interface can run in a browser,
/// the update feed that it checks and the folder of its data. A release build has none of these: its command
/// line does not know them.
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
}
