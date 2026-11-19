using Bravo.Core.Settings;
using Xunit;

namespace Bravo.Core.Tests.Settings;

public sealed class UserSettingsTests
{
    [Fact]
    public void Default_Settings_AreTheOnesOfAUserWhoChoseNothing()
    {
        var settings = UserSettings.Default;

        Assert.Equal(Theme.Auto, settings.Theme);
        Assert.Equal(DiagnosticLevel.None, settings.DiagnosticLevel);
        Assert.Equal(new ProxySettings { Type = ProxyType.System }, settings.Proxy);
    }
}
