using Bravo.Core.Policies;
using Bravo.Core.Settings;
using Bravo.Core.Telemetry;
using Xunit;

namespace Bravo.Core.Tests.Telemetry;

public sealed class TelemetryConsentTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Of_NoPolicy_FollowsTheOptionOfTheUser(bool option)
    {
        var consent = TelemetryConsent.Of(ApplicationPolicies.None, new UserSettings { TelemetryEnabled = option });

        Assert.Equal(new TelemetryConsent(option, TelemetryConsentSource.Option), consent);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Of_Policy_AppliesOverTheOptionOfTheUser(bool policy, bool option)
    {
        var policies = new ApplicationPolicies { TelemetryEnabled = policy };

        var consent = TelemetryConsent.Of(policies, new UserSettings { TelemetryEnabled = option });

        Assert.Equal(new TelemetryConsent(policy, TelemetryConsentSource.Policy), consent);
    }
}
