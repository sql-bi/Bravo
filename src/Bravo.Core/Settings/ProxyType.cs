namespace Bravo.Core.Settings;

public enum ProxyType
{
    /// <summary>
    /// No proxy server, even when the system has one.
    /// </summary>
    None,

    /// <summary>
    /// The proxy server that the system is configured with.
    /// </summary>
    System,

    /// <summary>
    /// The proxy server that the other proxy settings describe.
    /// </summary>
    Custom,
}
