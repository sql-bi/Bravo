using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        if (address is not null && !ProxySettings.IsServerAddress(address))
        {
            yield return new ValidationResult("The address is not the address of a server.", [nameof(Address)]);
        }

        foreach (var pattern in ProxySettings.FindInvalidBypassPatterns(BypassList))
        {
            var message = $"'{pattern}' is not a regular expression.";
            yield return new ValidationResult(message, [nameof(BypassList)]);
        }
    }

    private static string? NullIfBlank(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
