namespace Bravo.Desktop.Development;

/// <summary>
/// What a development build is asked for on its command line, so that its user interface can run in a
/// browser. A release build has none of these: its command line does not know them.
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
}
