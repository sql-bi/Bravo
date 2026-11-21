using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

        return TryCreateServerAddress(address, out _);
    }

    /// <summary>
    /// The entries of a bypass list that are not regular expressions. The entries are separated by semicolons,
    /// and an empty one is no entry.
    /// </summary>
    public static IReadOnlyList<string> FindInvalidBypassPatterns(string? bypassList)
    {
        return [.. SplitPatterns(bypassList).Where(pattern => !IsRegularExpression(pattern))];
    }

    /// <summary>
    /// The address of the proxy server as a connection uses it, or <see langword="null"/> when the settings
    /// have none. An address without a scheme is one of an HTTP proxy.
    /// </summary>
    public Uri? GetServerAddress()
    {
        if (string.IsNullOrWhiteSpace(Address) || !TryCreateServerAddress(Address, out var address))
        {
            return null;
        }

        return address;
    }

    /// <summary>
    /// The entries of the bypass list, each a regular expression.
    /// </summary>
    public IReadOnlyList<string> GetBypassPatterns()
    {
        return SplitPatterns(BypassList);
    }

    private static bool TryCreateServerAddress(string text, [NotNullWhen(true)] out Uri? address)
    {
        var absolute = text.Contains(Uri.SchemeDelimiter, StringComparison.Ordinal)
            ? text
            : $"{Uri.UriSchemeHttp}{Uri.SchemeDelimiter}{text}";

        return Uri.TryCreate(absolute, UriKind.Absolute, out address);
    }

    private static string[] SplitPatterns(string? bypassList)
    {
        return bypassList?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
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
