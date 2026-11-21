using System.Threading.Tasks;
using Bravo.Desktop.Network;
using Bravo.Desktop.Shell;
using Xunit;

namespace Bravo.Desktop.Tests.Network;

public sealed class ProxyCredentialDialogTests
{
    // Asked before the window is shown or after it was closed: no dialog box of Windows opens on its own.
    [Fact]
    public async Task ShowAsync_NoWindow_ShowsNothingAndReturnsNull()
    {
        var dialog = new ProxyCredentialDialog(new MainWindowDispatcher());

        var entered = await dialog.ShowAsync("test-user", TestContext.Current.CancellationToken);

        Assert.Null(entered);
    }
}
