namespace Bravo.Core.Settings;

public sealed record ProxySettings
{
    public ProxyType Type { get; init; } = ProxyType.System;

    /// <summary>
    /// Whether the credentials of the Windows user are sent to the proxy server.
    /// </summary>
    public bool UseDefaultCredentials { get; init; } = true;

    /// <summary>
    /// The address of the proxy server, read when <see cref="Type"/> is <see cref="ProxyType.Custom"/>.
    /// </summary>
    public string? Address { get; init; }

    public bool BypassOnLocal { get; init; } = true;

    /// <summary>
    /// The addresses that do not go through the proxy server: regular expressions separated by semicolons.
    /// </summary>
    public string? BypassList { get; init; }
}
