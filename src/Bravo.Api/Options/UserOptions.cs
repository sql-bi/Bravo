using System.Text.Json;
using Bravo.Core.Settings;

namespace Bravo.Api.Options;

// Public, as every type that travels: the validation and the descriptions of the document are generated
// for public types only.
/// <summary>
/// The options of the user, as the user interface reads them and replaces them.
/// </summary>
public sealed record UserOptions
{
    public required Theme Theme { get; init; }

    public required bool TelemetryEnabled { get; init; }

    public required bool CustomTemplatesEnabled { get; init; }

    public required bool UpdateCheckEnabled { get; init; }

    public required bool UseSystemBrowserForAuthentication { get; init; }

    public required DiagnosticLevel DiagnosticLevel { get; init; }

    public required ProxyOptions Proxy { get; init; }

    /// <summary>
    /// The options that only the user interface reads: any JSON value, returned as it was sent.
    /// </summary>
    public JsonElement? CustomOptions { get; init; }

    internal static UserOptions FromSettings(UserSettings settings)
    {
        return new UserOptions
        {
            Theme = settings.Theme,
            TelemetryEnabled = settings.TelemetryEnabled,
            CustomTemplatesEnabled = settings.CustomTemplatesEnabled,
            UpdateCheckEnabled = settings.UpdateCheckEnabled,
            UseSystemBrowserForAuthentication = settings.UseSystemBrowserForAuthentication,
            DiagnosticLevel = settings.DiagnosticLevel,
            Proxy = ProxyOptions.FromSettings(settings.Proxy),
            CustomOptions = settings.CustomOptions,
        };
    }

    internal UserSettings ToSettings()
    {
        return new UserSettings
        {
            Theme = Theme,
            TelemetryEnabled = TelemetryEnabled,
            CustomTemplatesEnabled = CustomTemplatesEnabled,
            UpdateCheckEnabled = UpdateCheckEnabled,
            UseSystemBrowserForAuthentication = UseSystemBrowserForAuthentication,
            DiagnosticLevel = DiagnosticLevel,
            Proxy = Proxy.ToSettings(),
            CustomOptions = CustomOptions,
        };
    }
}
