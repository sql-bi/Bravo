using System;
using Bravo.Platform.Windows.Browser;
using Xunit;

namespace Bravo.Platform.Windows.Tests.Browser;

// An address of a trusted site would open the browser of the machine: only the refusals are tested.
public sealed class DefaultBrowserLauncherTests
{
    [Theory]
    [InlineData("https://attacker.test/")]
    [InlineData("file:///C:/Windows/System32/calc.exe")]
    [InlineData("http://localhost/")]
    public void Open_AddressThatIsNotOnATrustedSite_ThrowsAndStartsNothing(string address)
    {
        var launcher = new DefaultBrowserLauncher();

        Assert.Throws<ArgumentException>(() => launcher.Open(new Uri(address)));
    }
}
