using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
using Sqlbi.Bravo.Infrastructure.Policies;
using Sqlbi.Bravo.Infrastructure.Telemetry;
using Xunit;

namespace Bravo.Tests.Infrastructure.Telemetry;

public class TelemetryConsentTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Resolve_WithoutPolicy_UsesTheUserSetting(bool settingValue)
    {
        var policies = PolicySnapshot.NotConfigured;
        var settings = new UserSettings { TelemetryEnabled = settingValue };

        var consent = TelemetryConsent.Resolve(policies, settings);

        Assert.Equal(settingValue, consent.IsEnabled);
        Assert.Equal(TelemetryConsentSource.UserSettings, consent.Source);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void Resolve_WithPolicy_OverridesTheUserSetting(bool policyValue, bool settingValue)
    {
        var policies = PolicySnapshot.NotConfigured with { TelemetryEnabled = policyValue };
        var settings = new UserSettings { TelemetryEnabled = settingValue };

        var consent = TelemetryConsent.Resolve(policies, settings);

        Assert.Equal(policyValue, consent.IsEnabled);
        Assert.Equal(TelemetryConsentSource.Policy, consent.Source);
    }
}