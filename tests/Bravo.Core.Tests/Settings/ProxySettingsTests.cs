using System;
using Bravo.Core.Settings;
using Xunit;

namespace Bravo.Core.Tests.Settings;

public sealed class ProxySettingsTests
{
    [Theory]
    [InlineData("proxy.contoso.com:8080")]
    [InlineData("http://proxy.contoso.com:8080")]
    [InlineData("https://10.0.0.1")]
    public void IsServerAddress_AddressWithOrWithoutScheme_IsTrue(string address)
    {
        Assert.True(ProxySettings.IsServerAddress(address));
    }

    [Theory]
    [InlineData("http://exa mple")]
    [InlineData("")]
    public void IsServerAddress_TextThatIsNotAnAddress_IsFalse(string address)
    {
        Assert.False(ProxySettings.IsServerAddress(address));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(@".*\.contoso\.com; 10\..*;")]
    public void FindInvalidBypassPatterns_RegularExpressionsOrNone_FindsNothing(string? bypassList)
    {
        Assert.Empty(ProxySettings.FindInvalidBypassPatterns(bypassList));
    }

    [Fact]
    public void FindInvalidBypassPatterns_EntryThatIsNotARegularExpression_FindsItTrimmed()
    {
        var invalid = ProxySettings.FindInvalidBypassPatterns("intranet; (unclosed ;other");

        Assert.Equal(["(unclosed"], invalid);
    }

    [Theory]
    [InlineData("proxy.contoso.com:8080", "http://proxy.contoso.com:8080/")]
    [InlineData("http://proxy.contoso.com:8080", "http://proxy.contoso.com:8080/")]
    [InlineData("socks5://10.0.0.1:1080", "socks5://10.0.0.1:1080/")]
    public void GetServerAddress_AddressWithOrWithoutScheme_IsTheAddressOfTheServer(string address, string expected)
    {
        var settings = new ProxySettings { Address = address };

        Assert.Equal(new Uri(expected), settings.GetServerAddress());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("http://exa mple")]
    public void GetServerAddress_NoAddress_IsNull(string? address)
    {
        var settings = new ProxySettings { Address = address };

        Assert.Null(settings.GetServerAddress());
    }

    [Fact]
    public void GetBypassPatterns_ListWithEmptyEntries_HasTheEntriesTrimmed()
    {
        var settings = new ProxySettings { BypassList = @".*\.contoso\.com; 10\..*;;" };

        Assert.Equal([@".*\.contoso\.com", @"10\..*"], settings.GetBypassPatterns());
    }

    [Fact]
    public void GetBypassPatterns_NoList_IsEmpty()
    {
        Assert.Empty(new ProxySettings().GetBypassPatterns());
    }
}
