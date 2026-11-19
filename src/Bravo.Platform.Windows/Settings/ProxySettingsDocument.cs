using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bravo.Core.Settings;

namespace Bravo.Platform.Windows.Settings;

/// <summary>
/// The member <c>proxy</c> of the file of the settings, in the order of 1.0.x.
/// </summary>
internal sealed record ProxySettingsDocument
{
    [JsonPropertyName("type")]
    public JsonElement? Type { get; init; }

    [JsonPropertyName("useDefaultCredentials")]
    public JsonElement? UseDefaultCredentials { get; init; }

    [JsonPropertyName("address")]
    public JsonElement? Address { get; init; }

    [JsonPropertyName("bypassOnLocal")]
    public JsonElement? BypassOnLocal { get; init; }

    [JsonPropertyName("bypassList")]
    public JsonElement? BypassList { get; init; }

    /// <summary>
    /// What a later version, or an earlier one, wrote and this one does not know: it is written back.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? UnknownMembers { get; init; }

    /// <summary>
    /// The proxy settings of the member. An address or a bypass list that the API would refuse makes them all
    /// the defaults, as 1.0.x reads them.
    /// </summary>
    public static ProxySettings ToSettings(JsonElement? member)
    {
        var document = Parse(member);
        var defaults = new ProxySettings();
        var settings = new ProxySettings
        {
            Type = DocumentMember.ToEnum(document.Type, defaults.Type),
            UseDefaultCredentials = DocumentMember.ToBoolean(document.UseDefaultCredentials, defaults.UseDefaultCredentials),
            Address = DocumentMember.ToText(document.Address),
            BypassOnLocal = DocumentMember.ToBoolean(document.BypassOnLocal, defaults.BypassOnLocal),
            BypassList = DocumentMember.ToText(document.BypassList),
        };

        var addressIsValid = settings.Address is null || ProxySettings.IsServerAddress(settings.Address);
        if (!addressIsValid || ProxySettings.FindInvalidBypassPatterns(settings.BypassList).Count > 0)
        {
            return defaults;
        }

        return settings;
    }

    /// <summary>
    /// The member that holds the proxy settings, with the members that the earlier one had and that this
    /// version does not know.
    /// </summary>
    public static JsonElement FromSettings(ProxySettings settings, JsonElement? previous)
    {
        var document = Parse(previous) with
        {
            Type = JsonSerializer.SerializeToElement(settings.Type),
            UseDefaultCredentials = JsonSerializer.SerializeToElement(settings.UseDefaultCredentials),
            Address = JsonSerializer.SerializeToElement(settings.Address),
            BypassOnLocal = JsonSerializer.SerializeToElement(settings.BypassOnLocal),
            BypassList = JsonSerializer.SerializeToElement(settings.BypassList),
        };

        return JsonSerializer.SerializeToElement(document);
    }

    // 1.0.x writes null until the user changes the proxy, and any other value that is not an object is none.
    private static ProxySettingsDocument Parse(JsonElement? member)
    {
        if (member is not { ValueKind: JsonValueKind.Object } value)
        {
            return new ProxySettingsDocument();
        }

        return value.Deserialize<ProxySettingsDocument>() ?? new ProxySettingsDocument();
    }
}
