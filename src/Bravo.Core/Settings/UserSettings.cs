using System.Text.Json;

namespace Bravo.Core.Settings;

/// <summary>
/// What the user chose in the options of the application. A value is never changed: a new one replaces it.
/// </summary>
public sealed record UserSettings
{
    /// <summary>
    /// The settings of a user who has chosen nothing yet.
    /// </summary>
    public static UserSettings Default { get; } = new();

    public Theme Theme { get; init; } = Theme.Auto;

    public bool TelemetryEnabled { get; init; } = true;

    public bool CustomTemplatesEnabled { get; init; } = true;

    public bool UpdateCheckEnabled { get; init; } = true;

    public bool UseSystemBrowserForAuthentication { get; init; }

    public DiagnosticLevel DiagnosticLevel { get; init; } = DiagnosticLevel.None;

    public ProxySettings Proxy { get; init; } = new();

    /// <summary>
    /// The options that only the user interface reads: the host keeps them as it received them.
    /// </summary>
    public JsonElement? CustomOptions { get; init; }
}
