using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Xunit;

namespace Bravo.Api.Tests.Network;

public sealed class NetworkEndpointsTests
{
    private const string PromptRoute = "/network/proxy-credentials/prompt";
    private const string CredentialsRoute = "/network/proxy-credentials";

    [Fact]
    public async Task PromptProxyCredentials_UserThatEntersCredentials_KeepsThemAndSaysSo()
    {
        await using var api = await TestApi.StartAsync();
        var entered = new NetworkCredential("test-user", "test-password");
        api.ProxyCredentialDialog.ShowAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(entered);

        using var response = await api.Client.PostAsync(PromptRoute, content: null, TestContext.Current.CancellationToken);

        var result = await response.ReadJsonAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(result.GetProperty("entered").GetBoolean());
        api.ProxyCredentialStore.Received(1).Write(entered);
    }

    [Fact]
    public async Task PromptProxyCredentials_UserThatCancels_SaysSoAndKeepsNothing()
    {
        await using var api = await TestApi.StartAsync();
        api.ProxyCredentialDialog.ShowAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns((NetworkCredential?)null);

        using var response = await api.Client.PostAsync(PromptRoute, content: null, TestContext.Current.CancellationToken);

        var result = await response.ReadJsonAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(result.GetProperty("entered").GetBoolean());
        api.ProxyCredentialStore.DidNotReceiveWithAnyArgs().Write(default!);
    }

    [Fact]
    public async Task DeleteProxyCredentials_Always_ForgetsTheCredentialsAndIsAnswered204()
    {
        await using var api = await TestApi.StartAsync();

        using var response = await api.Client.DeleteAsync(CredentialsRoute, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        api.ProxyCredentialStore.Received(1).Delete();
    }
}
