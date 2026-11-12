using System;
using Bravo.Desktop.WebView;
using Microsoft.Web.WebView2.Core;
using Xunit;

namespace Bravo.Desktop.Tests.WebView;

public sealed class WebViewPermissionRuleTests
{
    private const string DocumentOfThePage = "https://bravo.invalid/";

    // A value that the enum of the SDK does not define, as a later runtime may send.
    private const CoreWebView2PermissionKind KindThatTheSdkDoesNotName = (CoreWebView2PermissionKind)999;

    /// <summary>
    /// Every kind that the version of the SDK in use names.
    /// </summary>
    public static TheoryData<CoreWebView2PermissionKind> KindsThatTheSdkNames()
    {
        return [.. Enum.GetValues<CoreWebView2PermissionKind>()];
    }

    [Theory]
    [InlineData(CoreWebView2PermissionKind.ClipboardRead, "https://example.com/")]
    [InlineData(CoreWebView2PermissionKind.Camera, "https://example.com/")]
    [InlineData(KindThatTheSdkDoesNotName, "https://example.com/")]
    [InlineData(CoreWebView2PermissionKind.ClipboardRead, "http://bravo.invalid/")]
    [InlineData(CoreWebView2PermissionKind.ClipboardRead, "about:blank")]
    [InlineData(KindThatTheSdkDoesNotName, null)]
    public void Decide_RequestThatIsNotOfTheOriginOfThePage_IsDenied(CoreWebView2PermissionKind kind, string? address)
    {
        var state = WebViewPermissionRule.Decide(kind, address);

        Assert.Equal(CoreWebView2PermissionState.Deny, state);
    }

    [Fact]
    public void Decide_ClipboardReadOfTheOriginOfThePage_IsAllowed()
    {
        var state = WebViewPermissionRule.Decide(CoreWebView2PermissionKind.ClipboardRead, DocumentOfThePage);

        Assert.Equal(CoreWebView2PermissionState.Allow, state);
    }

    // The kinds are written out, here as in the rule: what is denied does not follow the enum of the SDK.
    [Theory]
    [InlineData(CoreWebView2PermissionKind.UnknownPermission)]
    [InlineData(CoreWebView2PermissionKind.Microphone)]
    [InlineData(CoreWebView2PermissionKind.Camera)]
    [InlineData(CoreWebView2PermissionKind.Geolocation)]
    [InlineData(CoreWebView2PermissionKind.Notifications)]
    [InlineData(CoreWebView2PermissionKind.OtherSensors)]
    [InlineData(CoreWebView2PermissionKind.MultipleAutomaticDownloads)]
    [InlineData(CoreWebView2PermissionKind.FileReadWrite)]
    [InlineData(CoreWebView2PermissionKind.Autoplay)]
    [InlineData(CoreWebView2PermissionKind.LocalFonts)]
    [InlineData(CoreWebView2PermissionKind.MidiSystemExclusiveMessages)]
    [InlineData(CoreWebView2PermissionKind.WindowManagement)]
    [InlineData(CoreWebView2PermissionKind.PersistentStorage)]
    public void Decide_KindThatThePageDoesNotUseOfTheOriginOfThePage_IsDenied(CoreWebView2PermissionKind kind)
    {
        var state = WebViewPermissionRule.Decide(kind, DocumentOfThePage);

        Assert.Equal(CoreWebView2PermissionState.Deny, state);
    }

    // The engine then asks the user: an answer of this version could stop the page after an update.
    [Fact]
    public void Decide_KindThatTheApplicationDoesNotKnowOfTheOriginOfThePage_IsNotDecidedByTheApplication()
    {
        var state = WebViewPermissionRule.Decide(KindThatTheSdkDoesNotName, DocumentOfThePage);

        Assert.Null(state);
    }

    // Fails when an update of the WebView2 package names a kind that the rule does not: until someone
    // decides, that kind is left to the engine, which may not be what the application wants.
    [Theory]
    [MemberData(nameof(KindsThatTheSdkNames))]
    public void Decide_EveryKindThatTheSdkNames_HasAnAnswerOfTheRule(CoreWebView2PermissionKind kind)
    {
        var state = WebViewPermissionRule.Decide(kind, DocumentOfThePage);

        Assert.True(
            state is not null,
            $"The WebView2 SDK names the permission {kind}, and WebViewPermissionRule has no answer for it. "
            + "Decide the answer for the page, write the kind in the rule, and the decision in docs/spec.md, "
            + "section \"Window and webview\".");
    }
}
