using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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

    /// <summary>
    /// Whether a text is the address of a proxy server. An address without a scheme is one of an HTTP proxy, as
    /// the system settings write it.
    /// </summary>
    public static bool IsServerAddress(string address)
    {
        ArgumentNullException.ThrowIfNull(address);

        var absolute = address.Contains(Uri.SchemeDelimiter, StringComparison.Ordinal)
            ? address
            : $"{Uri.UriSchemeHttp}{Uri.SchemeDelimiter}{address}";

        return Uri.TryCreate(absolute, UriKind.Absolute, out _);
    }

    /// <summary>
    /// The entries of a bypass list that are not regular expressions. The entries are separated by semicolons,
    /// and an empty one is no entry.
    /// </summary>
    public static IReadOnlyList<string> FindInvalidBypassPatterns(string? bypassList)
    {
        var patterns = bypassList?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return [.. (patterns ?? []).Where(pattern => !IsRegularExpression(pattern))];
    }

    private static bool IsRegularExpression(string pattern)
    {
        try
        {
            _ = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
