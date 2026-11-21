namespace Bravo.Api.Network;

// Public, as every type that travels: the validation and the descriptions of the document are generated
// for public types only.
/// <summary>
/// What the user did in the dialog that asks for the credentials of the proxy server.
/// </summary>
public sealed record ProxyCredentialsPromptResult
{
    /// <summary>
    /// Whether the user entered credentials, which the application keeps and answers a proxy server with from
    /// now on. False when the user cancelled the dialog, or the application has no window to show it in: the
    /// credentials that were kept, if any, stay as they were.
    /// </summary>
    public required bool Entered { get; init; }
}
