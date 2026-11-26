using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Bravo.Api.Security;
using Bravo.Api.Server;
using Bravo.Api.Tests;
using Bravo.Core.Errors;
using Bravo.Core.Policies;
using Bravo.Desktop.Development;
using Bravo.Desktop.Hosting;
using Bravo.Desktop.Telemetry;
using Bravo.Desktop.Tests.SingleInstance;
using Bravo.Platform.Windows.SingleInstance;
using Bravo.Platform.Windows.Tests.Network;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bravo.Desktop.Tests.Network;

/// <summary>
/// The host as the application builds it, with a proxy of the user in its settings: the update feed has a name
/// that only the proxy of the test answers, on the loopback address.
/// </summary>
public sealed class DesktopHostNetworkTests : IAsyncDisposable
{
    private static readonly Uri s_feedAddress = new(TestUpdateFeed.Address);
    private static readonly DevelopmentOptions s_withFeed = DevelopmentOptions.None with { UpdateFeedAddress = s_feedAddress };

    private readonly OwnedInstance _instance = new();
    private readonly TemporaryFolder _dataFolder = new();
    private readonly HttpClient _client = new(new SocketsHttpHandler { UseProxy = false }, disposeHandler: true);
    private WebApplication? _host;

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        if (_host is not null)
        {
            await _host.DisposeAsync();
        }

        _instance.Dispose();
        _dataFolder.Dispose();
    }

    [Fact]
    public async Task GetLatestRelease_ProxyOfTheUserInTheSettings_ReadsTheFeedThroughIt()
    {
        await using var proxy = new TestProxy { Body = TestUpdateFeed.Document };
        await StartHostAsync(proxy);

        using var response = await _client.GetAsync(TestUpdateFeed.LatestReleaseRoute, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var request = Assert.Single(proxy.Requests);
        Assert.StartsWith(s_feedAddress.AbsoluteUri, request.Target, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetLatestRelease_ProxyThatAsksForCredentials_IsAnswered409WithTheCodeOfTheCause()
    {
        await using var proxy = new TestProxy { Challenges = ["Basic realm=\"test\""] };
        await StartHostAsync(proxy);

        using var response = await _client.GetAsync(TestUpdateFeed.LatestReleaseRoute, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(nameof(ErrorCode.ProxyAuthenticationRequired), await response.ReadProblemCodeAsync());
    }

    // The proxy of a run is the one of its start: the options keep the change for the next one.
    [Fact]
    public async Task GetLatestRelease_ProxyChangedWhileTheHostRuns_StillGoesThroughTheProxyOfTheStart()
    {
        await using var proxy = new TestProxy { Body = TestUpdateFeed.Document };
        await StartHostAsync(proxy);
        var options = await _client.GetFromJsonAsync<JsonObject>("/options", TestContext.Current.CancellationToken);
        options!["proxy"]!["type"] = "None";
        using var saved = await _client.PutAsJsonAsync("/options", options, TestContext.Current.CancellationToken);

        using var response = await _client.GetAsync(TestUpdateFeed.LatestReleaseRoute, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(proxy.Requests);
    }

    private async Task StartHostAsync(TestProxy proxy)
    {
        await ProxySettingsFile.WriteAsync(_dataFolder, proxy.Address, useDefaultCredentials: true);

        _host = DesktopHost.Build(
            BuildIdentity.Development("1.1.0"),
            _dataFolder.AsDataFolder(),
            s_withFeed,
            ApplicationPolicies.None,
            _instance.Ownership,
            new SingleInstanceOptions(),
            _dataFolder.Log,
            TelemetrySession.Start());
        await _host.StartAsync(TestContext.Current.CancellationToken);

        var token = _host.Services.GetRequiredService<ApiToken>().Value;
        _client.BaseAddress = _host.Services.GetRequiredService<IApiAddressProvider>().GetAddress();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
