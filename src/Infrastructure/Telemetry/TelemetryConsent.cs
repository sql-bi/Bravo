using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
using Sqlbi.Bravo.Infrastructure.Policies;

namespace Sqlbi.Bravo.Infrastructure.Telemetry;

internal enum TelemetryConsentSource
{
    UserSettings,
    Policy,
}

/// <summary>
/// The effective decision on whether telemetry is collected, and where that decision comes from.
/// </summary>
internal readonly record struct TelemetryConsent(bool IsEnabled, TelemetryConsentSource Source)
{
    /// <remarks>
    /// A configured policy overrides the user preference: the source tells the user which one applies.
    /// </remarks>
    public static TelemetryConsent Resolve(PolicySnapshot policies, IUserSettings settings)
    {
        return policies.TelemetryEnabled is bool policyValue
            ? new TelemetryConsent(policyValue, TelemetryConsentSource.Policy)
            : new TelemetryConsent(settings.TelemetryEnabled, TelemetryConsentSource.UserSettings);
    }
}