using System;
using System.Linq;
using Bravo.Desktop.WebView;
using Microsoft.Web.WebView2.Core;
using Xunit;

namespace Bravo.Desktop.Tests.WebView;

public sealed class WebViewPermissionRuleTests
{
    private const string DocumentOfThePage = "https://bravo.invalid/";

    // A value that the enum of this version of the SDK does not define, as a later runtime may send.
    private const CoreWebView2PermissionKind KindThatTheSdkDoesNotName = (CoreWebView2PermissionKind)999;

    /// <summary>
    /// Every kind that the SDK names, but the one that the page is allowed.
    /// </summary>
    public static TheoryData<CoreWebView2PermissionKind> NamedKindsThatThePageDoesNotUse()
    {
        var kinds = Enum.GetValues<CoreWebView2PermissionKind>();
        return [.. kinds.Where(kind => kind != CoreWebView2PermissionKind.ClipboardRead)];
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

    [Theory]
    [MemberData(nameof(NamedKindsThatThePageDoesNotUse))]
    public void Decide_AnyOtherKindThatTheSdkNamesOfTheOriginOfThePage_IsDenied(CoreWebView2PermissionKind kind)
    {
        var state = WebViewPermissionRule.Decide(kind, DocumentOfThePage);

        Assert.Equal(CoreWebView2PermissionState.Deny, state);
    }

    // The kind that the engine uses for a permission it gives no other name to is a named one, and denied.
    [Fact]
    public void Decide_UnknownPermissionOfTheOriginOfThePage_IsDenied()
    {
        var state = WebViewPermissionRule.Decide(CoreWebView2PermissionKind.UnknownPermission, DocumentOfThePage);

        Assert.Equal(CoreWebView2PermissionState.Deny, state);
    }

    // The engine then asks the user: an answer of this version could stop the page after an update of the runtime.
    [Fact]
    public void Decide_KindThatTheSdkDoesNotNameOfTheOriginOfThePage_IsNotDecidedByTheApplication()
    {
        var state = WebViewPermissionRule.Decide(KindThatTheSdkDoesNotName, DocumentOfThePage);

        Assert.Null(state);
    }
}
