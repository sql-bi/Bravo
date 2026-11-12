using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Bravo.Core.Settings;

namespace Bravo.Api.Options;

public sealed record ProxyOptions : IValidatableObject
{
    public required ProxyType Type { get; init; }

    public required bool UseDefaultCredentials { get; init; }

    /// <summary>
    /// The address of the proxy server, with or without <c>http://</c>. Empty means none.
    /// </summary>
    public string? Address { get; init; }

    public required bool BypassOnLocal { get; init; }

    /// <summary>
    /// The addresses that do not go through the proxy server: regular expressions separated by semicolons.
    /// Empty means none.
    /// </summary>
    public string? BypassList { get; init; }

    internal static ProxyOptions FromSettings(ProxySettings settings)
    {
        return new ProxyOptions
        {
            Type = settings.Type,
            UseDefaultCredentials = settings.UseDefaultCredentials,
            Address = settings.Address,
            BypassOnLocal = settings.BypassOnLocal,
            BypassList = settings.BypassList,
        };
    }

    internal ProxySettings ToSettings()
    {
        return new ProxySettings
        {
            Type = Type,
            UseDefaultCredentials = UseDefaultCredentials,
            Address = NullIfBlank(Address),
            BypassOnLocal = BypassOnLocal,
            BypassList = NullIfBlank(BypassList),
        };
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var address = NullIfBlank(Address);
        if (address is not null && !IsServerAddress(address))
        {
            yield return new ValidationResult("The address is not the address of a server.", [nameof(Address)]);
        }

        var patterns = BypassList?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var pattern in patterns ?? [])
        {
            if (!IsRegularExpression(pattern))
            {
                var message = $"'{pattern}' is not a regular expression.";
                yield return new ValidationResult(message, [nameof(BypassList)]);
            }
        }
    }

    private static string? NullIfBlank(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    // An address without a scheme is one of an HTTP proxy, as the system settings write it.
    private static bool IsServerAddress(string address)
    {
        var absolute = address.Contains(Uri.SchemeDelimiter, StringComparison.Ordinal)
            ? address
            : $"{Uri.UriSchemeHttp}{Uri.SchemeDelimiter}{address}";

        return Uri.TryCreate(absolute, UriKind.Absolute, out _);
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
