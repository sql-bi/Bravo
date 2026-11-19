using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bravo.Core.Settings;

namespace Bravo.Platform.Windows.Settings;

/// <summary>
/// The file of the settings as 1.0.x writes and reads it: the members by their JSON names and in its order, the
/// enums as numbers, comments and trailing commas allowed. A member that is missing, of another type or out of
/// range is the default of its setting.
/// </summary>
internal sealed record UserSettingsDocument
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
    };

    [JsonPropertyName("telemetryEnabled")]
    public JsonElement? TelemetryEnabled { get; init; }

    [JsonPropertyName("diagnosticLevel")]
    public JsonElement? DiagnosticLevel { get; init; }

    [JsonPropertyName("updateCheckEnabled")]
    public JsonElement? UpdateCheckEnabled { get; init; }

    [JsonPropertyName("theme")]
    public JsonElement? Theme { get; init; }

    [JsonPropertyName("proxy")]
    public JsonElement? Proxy { get; init; }

    [JsonPropertyName("useSystemBrowserForAuthentication")]
    public JsonElement? UseSystemBrowserForAuthentication { get; init; }

    [JsonPropertyName("customTemplatesEnabled")]
    public JsonElement? CustomTemplatesEnabled { get; init; }

    [JsonPropertyName("customOptions")]
    public JsonElement? CustomOptions { get; init; }

    /// <summary>
    /// What a later version, or an earlier one, wrote and this one does not know, such as <c>updateChannel</c>
    /// of 1.0.x: it is written back, after the members that this version knows.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? UnknownMembers { get; init; }

    /// <exception cref="JsonException">The text is not a JSON object.</exception>
    public static UserSettingsDocument Parse(string text)
    {
        return JsonSerializer.Deserialize<UserSettingsDocument>(text, s_options)
            ?? throw new JsonException("The text is null, not an object.");
    }

    public byte[] ToUtf8Bytes()
    {
        return JsonSerializer.SerializeToUtf8Bytes(this, s_options);
    }

    public UserSettings ToSettings()
    {
        var defaults = UserSettings.Default;

        return new UserSettings
        {
            TelemetryEnabled = DocumentMember.ToBoolean(TelemetryEnabled, defaults.TelemetryEnabled),
            DiagnosticLevel = DocumentMember.ToEnum(DiagnosticLevel, defaults.DiagnosticLevel),
            UpdateCheckEnabled = DocumentMember.ToBoolean(UpdateCheckEnabled, defaults.UpdateCheckEnabled),
            Theme = DocumentMember.ToEnum(Theme, defaults.Theme),
            Proxy = ProxySettingsDocument.ToSettings(Proxy),
            UseSystemBrowserForAuthentication = DocumentMember.ToBoolean(
                UseSystemBrowserForAuthentication,
                defaults.UseSystemBrowserForAuthentication),
            CustomTemplatesEnabled = DocumentMember.ToBoolean(CustomTemplatesEnabled, defaults.CustomTemplatesEnabled),
            CustomOptions = CustomOptions is { ValueKind: not JsonValueKind.Null } options ? options : null,
        };
    }

    /// <summary>
    /// The document of the settings, with the members of this one that this version does not know.
    /// </summary>
    public UserSettingsDocument WithSettings(UserSettings settings)
    {
        return this with
        {
            TelemetryEnabled = JsonSerializer.SerializeToElement(settings.TelemetryEnabled),
            DiagnosticLevel = JsonSerializer.SerializeToElement(settings.DiagnosticLevel),
            UpdateCheckEnabled = JsonSerializer.SerializeToElement(settings.UpdateCheckEnabled),
            Theme = JsonSerializer.SerializeToElement(settings.Theme),
            Proxy = ProxySettingsDocument.FromSettings(settings.Proxy, Proxy),
            UseSystemBrowserForAuthentication = JsonSerializer.SerializeToElement(settings.UseSystemBrowserForAuthentication),
            CustomTemplatesEnabled = JsonSerializer.SerializeToElement(settings.CustomTemplatesEnabled),
            CustomOptions = settings.CustomOptions,
        };
    }
}
