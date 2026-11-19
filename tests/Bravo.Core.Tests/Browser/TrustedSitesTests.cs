using System;
using Bravo.Core.Browser;
using Xunit;

namespace Bravo.Core.Tests.Browser;

public sealed class TrustedSitesTests
{
    [Theory]
    [InlineData("https://bravo.bi/")]
    [InlineData("https://www.sqlbi.com/tv/")]
    [InlineData("https://docs.sqlbi.com/bravo/")]
    [InlineData("https://github.com/sql-bi/Bravo/releases/download/v1.1.0/Bravo.1.1.0.x64.msi")]
    [InlineData("https://GitHub.com/sql-bi/Bravo/issues/new")]
    [InlineData("https://learn.microsoft.com/power-bi/")]
    [InlineData("https://www.daxformatter.com/?embed=1")]
    [InlineData("https://bravorelease.blob.core.windows.net/bravo-public/currentversion.json")]
    [InlineData("https://code.visualstudio.com/")]
    [InlineData("https://marketplace.visualstudio.com/items?itemName=sqlbi.bravo")]
    [InlineData("http://sqlbi.com/")]
    public void Contains_WebPageOfATrustedSite_ReturnsTrue(string address)
    {
        Assert.True(TrustedSites.Contains(new Uri(address)));
    }

    [Theory]
    [InlineData("https://github.com.attacker.test/")]
    [InlineData("https://notsqlbi.com/")]
    [InlineData("https://attacker.test/github.com")]
    [InlineData("https://sqlbi.com@attacker.test/")]
    [InlineData("https://visualstudio.com/")]
    [InlineData("https://blob.core.windows.net/")]
    [InlineData("ftp://github.com/sql-bi/Bravo")]
    [InlineData("file:///C:/Windows/System32/calc.exe")]
    [InlineData(@"\\github.com\share\setup.exe")]
    [InlineData("mailto:support@sqlbi.com")]
    [InlineData("javascript:alert(1)")]
    [InlineData("http://localhost/")]
    [InlineData("http://127.0.0.1/")]
    public void Contains_AnyOtherAddress_ReturnsFalse(string address)
    {
        Assert.False(TrustedSites.Contains(new Uri(address)));
    }

    [Fact]
    public void Contains_RelativeAddress_ReturnsFalse()
    {
        Assert.False(TrustedSites.Contains(new Uri("github.com/sql-bi/Bravo", UriKind.Relative)));
    }
}
