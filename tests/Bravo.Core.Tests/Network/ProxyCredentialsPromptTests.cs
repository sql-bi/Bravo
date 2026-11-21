using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Core.Network;
using NSubstitute;
using Xunit;

namespace Bravo.Core.Tests.Network;

public sealed class ProxyCredentialsPromptTests
{
    private readonly IProxyCredentialDialog _dialog = Substitute.For<IProxyCredentialDialog>();
    private readonly IProxyCredentialStore _store = Substitute.For<IProxyCredentialStore>();
    private readonly ProxyCredentialsPrompt _prompt;

    public ProxyCredentialsPromptTests()
    {
        _prompt = new ProxyCredentialsPrompt(_dialog, _store);
    }

    [Fact]
    public async Task RunAsync_UserThatEntersCredentials_KeepsThem()
    {
        var entered = new NetworkCredential("test-user", "test-password");
        _dialog.ShowAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(entered);

        var isEntered = await _prompt.RunAsync(TestContext.Current.CancellationToken);

        Assert.True(isEntered);
        _store.Received(1).Write(entered);
    }

    [Fact]
    public async Task RunAsync_UserThatCancels_LeavesWhatWasKept()
    {
        _store.Read().Returns(new NetworkCredential("test-user", "test-password"));
        _dialog.ShowAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns((NetworkCredential?)null);

        var isEntered = await _prompt.RunAsync(TestContext.Current.CancellationToken);

        Assert.False(isEntered);
        _store.DidNotReceiveWithAnyArgs().Write(default!);
        _store.DidNotReceive().Delete();
    }

    [Fact]
    public async Task RunAsync_KeptCredentialsOfAUserOfADomain_OpensTheDialogWithThatUserName()
    {
        _store.Read().Returns(new NetworkCredential("test-user", "test-password", "CONTOSO"));

        await _prompt.RunAsync(TestContext.Current.CancellationToken);

        await _dialog.Received(1).ShowAsync(@"CONTOSO\test-user", TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task RunAsync_NoKeptCredentials_OpensTheDialogWithNoUserName()
    {
        _store.Read().Returns((NetworkCredential?)null);

        await _prompt.RunAsync(TestContext.Current.CancellationToken);

        await _dialog.Received(1).ShowAsync(null, TestContext.Current.CancellationToken);
    }
}
