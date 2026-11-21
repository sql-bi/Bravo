using System.Net;

namespace Bravo.Platform.Windows.Network;

/// <summary>
/// What a selection of the proxy is built with, beside the settings of the user.
/// </summary>
public sealed record ProxySelectionOptions
{
    /// <summary>
    /// The proxy that stands for the one of the system. The application sets none, and its connections use
    /// the one that .NET finds; a test gives its own, so that it never depends on the machine that runs it.
    /// </summary>
    public IWebProxy? SystemProxy { get; init; }
}
