using System;
using System.Net;
using System.Threading.Tasks;
using Bravo.Api.Tests;
using Bravo.Core.Errors;
using Bravo.Platform.Windows.Tests.Network;
using Xunit;

namespace Bravo.Desktop.Tests.Network;

/// <summary>
/// The executable with the proxy of the system, which no test can give the host in its own process: .NET takes
/// it from the environment variable <c>HTTP_PROXY</c> of the process, before it looks at the settings of
/// Windows. The proxy is the one of the test, on the loopback address, and the update feed has a name that
/// only it answers.
/// </summary>
[Collection(BravoExecutable.Collection)]
public sealed class BravoNetworkTests : IDisposable
{
    private readonly TemporaryFolder _dataFolder = new();

    public void Dispose()
    {
        _dataFolder.Dispose();
    }

    [Fact]
    public async Task GetLatestRelease_ProxyOfTheSystem_ReadsTheFeedThroughIt()
    {
        await using var proxy = new TestProxy { Body = TestUpdateFeed.Document };
        using var bravo = await BravoWithApi.StartAsync(_dataFolder, proxy.Address);

        using var response = await bravo.Client.GetAsync(TestUpdateFeed.LatestReleaseRoute, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(proxy.Requests, request => request.Target.StartsWith(TestUpdateFeed.Address, StringComparison.Ordinal));
    }

    // The Windows user never sends a password: the proxy gets no credentials, and the user is told to enter them.
    [Fact]
    public async Task GetLatestRelease_ProxyOfTheSystemThatAsksWithBasic_IsAnswered409AndGetsNoCredentials()
    {
        await using var proxy = new TestProxy { Challenges = ["Basic realm=\"test\""] };
        using var bravo = await BravoWithApi.StartAsync(_dataFolder, proxy.Address);

        using var response = await bravo.Client.GetAsync(TestUpdateFeed.LatestReleaseRoute, TestContext.Current.CancellationToken);

        Assert.Equal(nameof(ErrorCode.ProxyAuthenticationRequired), await response.ReadProblemCodeAsync());
        Assert.All(proxy.Requests, request => Assert.Null(request.AuthorizationScheme));
    }

    // The first message of the exchange is all that the proxy of the test sees: it holds no secret.
    [Fact]
    public async Task GetLatestRelease_ProxyOfTheSystemThatAsksWithNegotiate_IsAnsweredAsTheWindowsUser()
    {
        await using var proxy = new TestProxy { Challenges = ["Negotiate", "NTLM"] };
        using var bravo = await BravoWithApi.StartAsync(_dataFolder, proxy.Address);

        using var response = await bravo.Client.GetAsync(TestUpdateFeed.LatestReleaseRoute, TestContext.Current.CancellationToken);

        Assert.Equal(nameof(ErrorCode.ProxyAuthenticationRequired), await response.ReadProblemCodeAsync());
        Assert.Contains(proxy.Requests, request => request.AuthorizationScheme == "Negotiate");
    }
}
