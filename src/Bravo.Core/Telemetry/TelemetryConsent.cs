using System;
using Bravo.Core.Policies;
using Bravo.Core.Settings;

namespace Bravo.Core.Telemetry;

/// <summary>
/// Whether the application sends its telemetry, and who decided it.
/// </summary>
/// <param name="IsGiven">True when the application sends it.</param>
/// <param name="Source">The policy of an administrator, or the option of the user when no policy is set.</param>
public readonly record struct TelemetryConsent(bool IsGiven, TelemetryConsentSource Source)
{
    public static TelemetryConsent Of(ApplicationPolicies policies, UserSettings settings)
    {
        ArgumentNullException.ThrowIfNull(policies);
        ArgumentNullException.ThrowIfNull(settings);

        return policies.TelemetryEnabled is { } policy
            ? new TelemetryConsent(policy, TelemetryConsentSource.Policy)
            : new TelemetryConsent(settings.TelemetryEnabled, TelemetryConsentSource.Option);
    }
}
