using System;
using Bravo.Core.Settings;
using Xunit;

namespace Bravo.Core.Tests.Settings;

public sealed class InMemoryUserSettingsStoreTests
{
    [Fact]
    public void Current_NothingReplacedYet_IsTheDefaults()
    {
        var store = new InMemoryUserSettingsStore();

        Assert.Same(UserSettings.Default, store.Current);
    }

    [Fact]
    public void Replace_NewSettings_BecomeTheCurrentOnes()
    {
        var store = new InMemoryUserSettingsStore();
        var settings = UserSettings.Default with { Theme = Theme.Dark, TelemetryEnabled = false };

        store.Replace(settings);

        Assert.Same(settings, store.Current);
    }

    [Fact]
    public void Replace_Null_Throws()
    {
        var store = new InMemoryUserSettingsStore();

        Assert.Throws<ArgumentNullException>(() => store.Replace(null!));
    }

    [Fact]
    public void Default_Settings_AreTheOnesOfAUserWhoChoseNothing()
    {
        var settings = UserSettings.Default;

        Assert.Equal(Theme.Auto, settings.Theme);
        Assert.Equal(DiagnosticLevel.None, settings.DiagnosticLevel);
        Assert.Equal(new ProxySettings { Type = ProxyType.System }, settings.Proxy);
    }
}
