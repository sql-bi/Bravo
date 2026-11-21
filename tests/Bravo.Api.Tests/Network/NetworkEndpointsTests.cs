using System;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Core.Errors;
using Bravo.Core.Network;
using NSubstitute;
using Xunit;

namespace Bravo.Api.Tests.Network;

public sealed class NetworkEndpointsTests
{
    private const string TestRoute = "/network/test";

    private static readonly Uri s_service = new("https://service.bravo.test/");
    private static readonly Uri s_feed = new("https://feed.bravo.test/currentversion.json");

    [Fact]
    public async Task TestConnection_SiteReachedThroughAProxyAndSiteThatDoesNotAnswer_ReturnsBothAsResults()
    {
        await using var api = await TestApi.StartAsync();
        var throughProxy = new SiteProbeResult(new Uri("http://proxy.bravo.test:8080/"), Failure: null);
        var unreachable = new SiteProbeResult(Proxy: null, ErrorCode.ServerUnreachable);
        api.SiteProbe.ProbeAsync(s_service, Arg.Any<CancellationToken>()).Returns(throughProxy);
        api.SiteProbe.ProbeAsync(s_feed, Arg.Any<CancellationToken>()).Returns(unreachable);

        using var response = await api.Client.PostAsync(TestRoute, content: null, TestContext.Current.CancellationToken);

        var expected = """
            {
              "sites": [
                { "site": "PowerBIService", "address": "https://service.bravo.test/", "proxyAddress": "http://proxy.bravo.test:8080/" },
                { "site": "UpdateFeed", "address": "https://feed.bravo.test/currentversion.json", "failure": "ServerUnreachable" }
              ]
            }
            """;
        var actual = await response.ReadJsonAsync();
        using var expectedDocument = JsonDocument.Parse(expected);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(JsonElement.DeepEquals(expectedDocument.RootElement, actual), actual.GetRawText());
    }

    [Fact]
    public async Task TestConnection_WithoutTheToken_IsAnswered401AndAsksNoSite()
    {
        await using var api = await TestApi.StartAsync();
        using var anonymous = api.CreateAnonymousClient();

        using var response = await anonymous.PostAsync(TestRoute, content: null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        _ = api.SiteProbe.DidNotReceiveWithAnyArgs().ProbeAsync(default!, TestContext.Current.CancellationToken);
    }
}
