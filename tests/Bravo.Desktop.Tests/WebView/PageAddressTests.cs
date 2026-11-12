using Bravo.Desktop.WebView;
using Xunit;

namespace Bravo.Desktop.Tests.WebView;

public sealed class PageAddressTests
{
    [Theory]
    [InlineData("https://bravo.invalid")]
    [InlineData("https://bravo.invalid/")]
    [InlineData("https://bravo.invalid/index.html")]
    [InlineData("https://bravo.invalid/js/main.js?v=1#top")]
    [InlineData("https://BRAVO.invalid/index.html")]
    [InlineData("https://bravo.invalid:443/index.html")]
    public void Contains_AddressOfTheOriginOfThePage_IsTrue(string address)
    {
        Assert.True(PageAddress.Contains(address));
    }

    [Theory]
    [InlineData("http://bravo.invalid/index.html")]
    [InlineData("https://bravo.invalid:8443/index.html")]
    [InlineData("https://bravo.invalid.example.com/index.html")]
    [InlineData("https://www.bravo.invalid/index.html")]
    [InlineData("https://bravo.invalid@example.com/index.html")]
    [InlineData("https://example.com/?next=https://bravo.invalid/")]
    [InlineData("blob:https://bravo.invalid/0d6c6e0e-6d0b-4a6b-9a53-2b1b2f3c4d5e")]
    [InlineData("data:text/html,<p>bravo.invalid</p>")]
    [InlineData("file:///C:/Windows/win.ini")]
    [InlineData("about:blank")]
    [InlineData("/index.html")]
    [InlineData("")]
    [InlineData(null)]
    public void Contains_AnyOtherAddress_IsFalse(string? address)
    {
        Assert.False(PageAddress.Contains(address));
    }

    [Fact]
    public void Start_Address_IsOfTheOriginOfThePage()
    {
        Assert.True(PageAddress.Contains(PageAddress.Start));
    }
}
