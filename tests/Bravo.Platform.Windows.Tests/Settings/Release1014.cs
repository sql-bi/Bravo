using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bravo.Platform.Windows.Tests.Settings;

/// <summary>
/// The settings file of 1.0.14: two files as it writes them, with the line ends of Windows, and its reader, with
/// its types, its names and its options, without the policies.
/// </summary>
internal static class Release1014
{
    /// <summary>
    /// A user who chose the dark theme and nothing else: 1.0.14 writes null for a proxy never changed.
    /// </summary>
    public const string ThemeOnlyFile = """
        {
          "telemetryEnabled": true,
          "diagnosticLevel": 0,
          "updateChannel": 0,
          "updateCheckEnabled": true,
          "theme": 2,
          "proxy": null,
          "useSystemBrowserForAuthentication": false,
          "customTemplatesEnabled": true,
          "customOptions": null
        }
        """;

    /// <summary>
    /// A user who changed every setting, with custom options that the page of 1.0.14 sends.
    /// </summary>
    public const string EverythingChangedFile = """
        {
          "telemetryEnabled": false,
          "diagnosticLevel": 2,
          "updateChannel": 2,
          "updateCheckEnabled": false,
          "theme": 1,
          "proxy": {
            "type": 2,
            "useDefaultCredentials": false,
            "address": "proxy.contoso.com:8080",
            "bypassOnLocal": false,
            "bypassList": ".*\\.contoso\\.com;10\\..*"
          },
          "useSystemBrowserForAuthentication": true,
          "customTemplatesEnabled": false,
          "customOptions": {
            "sidebarCollapsed": true,
            "locale": "it-IT",
            "templates": [
              {
                "name": "Citt\u00E0 \u0026 \u003Ccalendario\u003E",
                "path": "C:\\Templates\\it.json",
                "enabled": true
              }
            ]
          }
        }
        """;

    private static readonly JsonSerializerOptions s_options = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
    };

    /// <exception cref="JsonException">1.0.14 cannot read the text, and starts with the defaults.</exception>
    public static Settings? Read(string text)
    {
        return JsonSerializer.Deserialize<Settings>(text, s_options);
    }

    internal sealed class Settings
    {
        [JsonPropertyName("telemetryEnabled")]
        public bool TelemetryEnabled { get; set; } = true;

        [JsonPropertyName("diagnosticLevel")]
        public DiagnosticLevelType DiagnosticLevel { get; set; } = DiagnosticLevelType.None;

        [JsonPropertyName("updateChannel")]
        public UpdateChannelType UpdateChannel { get; set; } = UpdateChannelType.Stable;

        [JsonPropertyName("updateCheckEnabled")]
        public bool UpdateCheckEnabled { get; set; } = true;

        [JsonPropertyName("theme")]
        public ThemeType Theme { get; set; } = ThemeType.Auto;

        [JsonPropertyName("proxy")]
        public ProxySettings? Proxy { get; set; }

        [JsonPropertyName("useSystemBrowserForAuthentication")]
        public bool UseSystemBrowserForAuthentication { get; set; }

        [JsonPropertyName("customTemplatesEnabled")]
        public bool CustomTemplatesEnabled { get; set; } = true;

        [JsonPropertyName("customOptions")]
        public JsonElement? CustomOptions { get; set; }
    }

    internal sealed class ProxySettings
    {
        [JsonPropertyName("type")]
        public ProxyType Type { get; set; } = ProxyType.System;

        [JsonPropertyName("useDefaultCredentials")]
        public bool UseDefaultCredentials { get; set; } = true;

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("bypassOnLocal")]
        public bool BypassOnLocal { get; set; } = true;

        [JsonPropertyName("bypassList")]
        public string? BypassList { get; set; }
    }

    internal enum ThemeType
    {
        Auto = 0,
        Light = 1,
        Dark = 2,
    }

    internal enum UpdateChannelType
    {
        Stable = 0,
        Dev = 2,
    }

    internal enum DiagnosticLevelType
    {
        None = 0,
        Basic = 1,
        Verbose = 2,
    }

    internal enum ProxyType
    {
        None = 0,
        System = 1,
        Custom = 2,
    }
}
