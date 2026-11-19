using System.Text;
using System.Text.Json;
using Bravo.Core.Settings;
using Bravo.Platform.Windows.Settings;
using Xunit;

namespace Bravo.Platform.Windows.Tests.Settings;

public sealed class UserSettingsDocumentTests
{
    private static readonly UserSettings s_everythingChanged = new()
    {
        TelemetryEnabled = false,
        DiagnosticLevel = DiagnosticLevel.Verbose,
        UpdateCheckEnabled = false,
        Theme = Theme.Light,
        Proxy = new ProxySettings
        {
            Type = ProxyType.Custom,
            UseDefaultCredentials = false,
            Address = "proxy.contoso.com:8080",
            BypassOnLocal = false,
            BypassList = @".*\.contoso\.com;10\..*",
        },
        UseSystemBrowserForAuthentication = true,
        CustomTemplatesEnabled = false,
    };

    [Fact]
    public void ToSettings_FileOf1014WithEverythingChanged_IsEverySetting()
    {
        var settings = UserSettingsDocument.Parse(Release1014.EverythingChangedFile).ToSettings();

        Assert.Equal(s_everythingChanged, settings with { CustomOptions = null });
    }

    [Fact]
    public void ToSettings_FileOf1014WithEverythingChanged_KeepsTheCustomOptionsAsTheyAre()
    {
        var expected = JsonDocument.Parse(Release1014.EverythingChangedFile).RootElement.GetProperty("customOptions");

        var settings = UserSettingsDocument.Parse(Release1014.EverythingChangedFile).ToSettings();

        Assert.True(JsonElement.DeepEquals(expected, settings.CustomOptions!.Value));
    }

    [Fact]
    public void ToSettings_FileOf1014WithANullProxy_IsTheDefaultProxy()
    {
        var settings = UserSettingsDocument.Parse(Release1014.ThemeOnlyFile).ToSettings();

        Assert.Equal(UserSettings.Default with { Theme = Theme.Dark }, settings);
    }

    [Fact]
    public void ToSettings_EmptyObject_IsTheDefaults()
    {
        Assert.Equal(UserSettings.Default, UserSettingsDocument.Parse("{}").ToSettings());
    }

    [Theory]
    [InlineData("""{ "theme": "Dark" }""")]
    [InlineData("""{ "theme": 7 }""")]
    [InlineData("""{ "theme": 1.5 }""")]
    [InlineData("""{ "theme": null }""")]
    [InlineData("""{ "telemetryEnabled": "false" }""")]
    [InlineData("""{ "telemetryEnabled": 0 }""")]
    [InlineData("""{ "diagnosticLevel": -1 }""")]
    [InlineData("""{ "proxy": "System" }""")]
    [InlineData("""{ "proxy": { "type": 9 } }""")]
    [InlineData("""{ "Theme": 2 }""")]
    public void ToSettings_MemberOfAnotherTypeOrOutOfRange_IsItsDefault(string text)
    {
        Assert.Equal(UserSettings.Default, UserSettingsDocument.Parse(text).ToSettings());
    }

    [Fact]
    public void ToSettings_CommentsAndTrailingCommas_AreRead()
    {
        var text = """
            {
              // Written by hand
              "theme": 2, /* dark */
            }
            """;

        Assert.Equal(Theme.Dark, UserSettingsDocument.Parse(text).ToSettings().Theme);
    }

    [Theory]
    [InlineData("""{ "proxy": { "type": 2, "address": "http://exa mple" } }""")]
    [InlineData("""{ "proxy": { "type": 2, "bypassList": "intranet;(unclosed" } }""")]
    public void ToSettings_ProxyThatTheApiWouldRefuse_IsTheDefaultProxy(string text)
    {
        Assert.Equal(new ProxySettings(), UserSettingsDocument.Parse(text).ToSettings().Proxy);
    }

    [Fact]
    public void ToSettings_BlankProxyAddress_IsNoAddress()
    {
        var settings = UserSettingsDocument.Parse("""{ "proxy": { "type": 2, "address": "  " } }""").ToSettings();

        Assert.Equal(new ProxySettings { Type = ProxyType.Custom }, settings.Proxy);
    }

    [Theory]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("""{ "theme": 2""")]
    public void Parse_TextThatIsNoObject_Throws(string text)
    {
        Assert.ThrowsAny<JsonException>(() => UserSettingsDocument.Parse(text));
    }

    [Fact]
    public void ToUtf8Bytes_EverySetting_IsReadBy1014AsTheSameSettings()
    {
        var bytes = new UserSettingsDocument().WithSettings(s_everythingChanged).ToUtf8Bytes();

        var read = Release1014.Read(Encoding.UTF8.GetString(bytes))!;

        Assert.Equal(
            (false, Release1014.DiagnosticLevelType.Verbose, false, Release1014.ThemeType.Light, true, false),
            (read.TelemetryEnabled, read.DiagnosticLevel, read.UpdateCheckEnabled, read.Theme,
                read.UseSystemBrowserForAuthentication, read.CustomTemplatesEnabled));
        Assert.Equal(
            (Release1014.ProxyType.Custom, false, "proxy.contoso.com:8080", false, @".*\.contoso\.com;10\..*"),
            (read.Proxy!.Type, read.Proxy.UseDefaultCredentials, read.Proxy.Address, read.Proxy.BypassOnLocal,
                read.Proxy.BypassList));
    }

    [Fact]
    public void ToUtf8Bytes_SettingsOfAFileOf1014_WriteBackTheMembersThatAreNoSetting()
    {
        var document = UserSettingsDocument.Parse(Release1014.EverythingChangedFile);

        var bytes = document.WithSettings(UserSettings.Default).ToUtf8Bytes();

        Assert.Equal(Release1014.UpdateChannelType.Dev, Release1014.Read(Encoding.UTF8.GetString(bytes))!.UpdateChannel);
    }

    [Fact]
    public void ToUtf8Bytes_ProxyWithAMemberThatIsNoSetting_WritesItBack()
    {
        var document = UserSettingsDocument.Parse("""{ "proxy": { "type": 1, "addedLater": [1, 2] } }""");

        var bytes = document.WithSettings(UserSettings.Default).ToUtf8Bytes();

        using var written = JsonDocument.Parse(bytes);
        using var expected = JsonDocument.Parse("[1, 2]");
        var addedLater = written.RootElement.GetProperty("proxy").GetProperty("addedLater");
        Assert.True(JsonElement.DeepEquals(expected.RootElement, addedLater));
    }

    // As 1.0.14 writes a file, with the proxy that it would write after a change and its update channel last.
    [Fact]
    public void ToUtf8Bytes_FileOf1014WrittenBack_IsItsTextWithTheProxyAndTheMemberThatIsNoSettingLast()
    {
        var expected = """
            {
              "telemetryEnabled": true,
              "diagnosticLevel": 0,
              "updateCheckEnabled": true,
              "theme": 2,
              "proxy": {
                "type": 1,
                "useDefaultCredentials": true,
                "address": null,
                "bypassOnLocal": true,
                "bypassList": null
              },
              "useSystemBrowserForAuthentication": false,
              "customTemplatesEnabled": true,
              "customOptions": null,
              "updateChannel": 0
            }
            """;
        var document = UserSettingsDocument.Parse(Release1014.ThemeOnlyFile);

        var bytes = document.WithSettings(document.ToSettings()).ToUtf8Bytes();

        Assert.Equal(expected, Encoding.UTF8.GetString(bytes));
    }
}
