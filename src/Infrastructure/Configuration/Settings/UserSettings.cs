using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Infrastructure.Configuration.Settings;

public interface IUserSettings
{
    bool TelemetryEnabled { get; set; }

    DiagnosticLevelType DiagnosticLevel { get; set; }

    bool UpdateCheckEnabled { get; set; }

    ThemeType Theme { get; set; }

    ProxySettings? Proxy { get; set; }

    bool UseSystemBrowserForAuthentication { get; set; }

    bool CustomTemplatesEnabled { get; set; }

    JsonElement? CustomOptions { get; set; }
}

public class UserSettings : IUserSettings
{
    public const bool DefaultTelemetryEnabled = true;
    public const DiagnosticLevelType DefaultDiagnosticLevel = DiagnosticLevelType.None;
    public const bool DefaultUpdateCheckEnabled = true;
    public const ThemeType DefaultTheme = ThemeType.Auto;
    public const bool DefaultUseSystemBrowserForAuthentication = false;
    public const bool DefaultCustomTemplatesEnabled = true;

    [JsonPropertyName("telemetryEnabled")]
    public bool TelemetryEnabled { get; set; } = DefaultTelemetryEnabled;

    [JsonPropertyName("diagnosticLevel")]
    public DiagnosticLevelType DiagnosticLevel { get; set; } = DefaultDiagnosticLevel;

    [JsonPropertyName("updateCheckEnabled")]
    public bool UpdateCheckEnabled { get; set; } = DefaultUpdateCheckEnabled;

    [JsonPropertyName("theme")]
    public ThemeType Theme { get; set; } = DefaultTheme;

    [JsonPropertyName("proxy")]
    public ProxySettings? Proxy { get; set; }

    [JsonPropertyName("useSystemBrowserForAuthentication")]
    public bool UseSystemBrowserForAuthentication { get; set; } = DefaultUseSystemBrowserForAuthentication;

    [JsonPropertyName("customTemplatesEnabled")]
    public bool CustomTemplatesEnabled { get; set; } = DefaultCustomTemplatesEnabled;

    [JsonPropertyName("customOptions")]
    public JsonElement? CustomOptions { get; set; }
}

public enum ThemeType
{
    Auto = 0,
    Light = 1,
    Dark = 2
}

public enum DiagnosticLevelType
{
    None = 0,
    Basic = 1,
    Verbose = 2
}
