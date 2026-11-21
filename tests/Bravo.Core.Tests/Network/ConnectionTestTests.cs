using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Core.Errors;
using Bravo.Core.Network;
using Bravo.Core.Policies;
using Bravo.Core.Updates;
using NSubstitute;
using Xunit;

namespace Bravo.Core.Tests.Network;

public sealed class ConnectionTestTests
{
    private static readonly Uri s_signIn = new("https://signin.bravo.test/");
    private static readonly Uri s_service = new("https://service.bravo.test/");
    private static readonly Uri s_feed = new("https://feed.bravo.test/currentversion.json");
    private static readonly Uri s_proxy = new("http://proxy.bravo.test:8080/");

    private readonly ISiteProbe _probe = Substitute.For<ISiteProbe>();

    public ConnectionTestTests()
    {
        var reached = new SiteProbeResult(Proxy: null, Failure: null);
        _probe.ProbeAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>()).Returns(reached);
    }

    [Fact]
    public async Task RunAsync_SitesAndAFeed_AsksTheSitesThenTheFeedAndKeepsTheirOrder()
    {
        var test = CreateTest(s_feed, ApplicationPolicies.None);

        var connections = await test.RunAsync(TestContext.Current.CancellationToken);

        NetworkSite[] expected = [NetworkSite.SignIn, NetworkSite.PowerBIService, NetworkSite.UpdateFeed];
        Assert.Equal(expected, connections.Select(connection => connection.Site));
        Assert.Equal([s_signIn, s_service, s_feed], connections.Select(connection => connection.Address));
    }

    [Fact]
    public async Task RunAsync_SiteThatDoesNotAnswer_TellsItsCauseAndItsProxyAndStillAsksTheOthers()
    {
        var unreachable = new SiteProbeResult(s_proxy, ErrorCode.ProxyAuthenticationRequired);
        _probe.ProbeAsync(s_signIn, Arg.Any<CancellationToken>()).Returns(unreachable);
        var test = CreateTest(feedAddress: null, ApplicationPolicies.None);

        var connections = await test.RunAsync(TestContext.Current.CancellationToken);

        var failed = new SiteConnection(NetworkSite.SignIn, s_signIn, s_proxy, ErrorCode.ProxyAuthenticationRequired);
        var reached = new SiteConnection(NetworkSite.PowerBIService, s_service, Proxy: null, Failure: null);
        Assert.Equal([failed, reached], connections);
    }

    // The policy stops every request to the feed: the one of the test too.
    [Fact]
    public async Task RunAsync_UpdateCheckTurnedOffByPolicy_SendsNoRequestToTheFeed()
    {
        var test = CreateTest(s_feed, new ApplicationPolicies { UpdateCheckEnabled = false });

        var connections = await test.RunAsync(TestContext.Current.CancellationToken);

        Assert.DoesNotContain(connections, connection => connection.Site == NetworkSite.UpdateFeed);
        _ = _probe.DidNotReceive().ProbeAsync(s_feed, Arg.Any<CancellationToken>());
    }

    // A slow site does not keep the others waiting.
    [Fact]
    public async Task RunAsync_SiteThatHasNotAnsweredYet_HasAlreadyAskedTheOthers()
    {
        var pending = new TaskCompletionSource<SiteProbeResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _probe.ProbeAsync(s_signIn, Arg.Any<CancellationToken>()).Returns(pending.Task);
        var test = CreateTest(feedAddress: null, ApplicationPolicies.None);

        var running = test.RunAsync(TestContext.Current.CancellationToken);

        _ = _probe.Received(1).ProbeAsync(s_service, Arg.Any<CancellationToken>());
        pending.SetResult(new SiteProbeResult(Proxy: null, Failure: null));
        var connections = await running;
        Assert.Equal(2, connections.Count);
    }

    private ConnectionTest CreateTest(Uri? feedAddress, ApplicationPolicies policies)
    {
        var options = new ConnectionTestOptions
        {
            Sites = [new(NetworkSite.SignIn, s_signIn), new(NetworkSite.PowerBIService, s_service)],
        };
        var updates = new UpdateCheckOptions
        {
            FeedAddress = feedAddress,
            FileVersion = new Version(1, 1, 0, 0),
            IncludesRuntime = true,
        };

        return new ConnectionTest(_probe, options, updates, policies);
    }
}
