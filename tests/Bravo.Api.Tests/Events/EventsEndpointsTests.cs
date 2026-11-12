using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Bravo.Core.Activation;
using Xunit;

namespace Bravo.Api.Tests.Events;

public sealed class EventsEndpointsTests
{
    private static readonly ActivationRequest s_reportOpened = new()
    {
        ServerName = "localhost:12345",
        DatabaseName = "db",
        ParentProcessId = 4242,
    };

    [Fact]
    public async Task GetEvents_ActivationPublishedBeforeThePageAsks_ReturnsItWithWhatTheCommandLineGave()
    {
        await using var api = await TestApi.StartAsync();
        api.Events.PublishActivation(s_reportOpened);

        using var response = await api.Client.GetAsync("/events", TestContext.Current.CancellationToken);

        using var expected = JsonDocument.Parse("""
            {
              "events": [
                {
                  "number": 1,
                  "kind": "Activation",
                  "activation": { "serverName": "localhost:12345", "databaseName": "db", "errors": [] }
                }
              ],
              "next": 1
            }
            """);
        var actual = await response.ReadJsonAsync();
        Assert.True(JsonElement.DeepEquals(expected.RootElement, actual), actual.GetRawText());
    }

    [Fact]
    public async Task GetEvents_StartWithNoArgumentsAndOneWithErrors_ReturnsTheErrorsAndNoMemberWithoutAValue()
    {
        await using var api = await TestApi.StartAsync();
        api.Events.PublishActivation(ActivationRequest.Empty);
        api.Events.PublishActivation(new ActivationRequest { CommandLineErrors = ["Option '--server' is required."] });

        using var response = await api.Client.GetAsync("/events", TestContext.Current.CancellationToken);

        using var expected = JsonDocument.Parse("""
            [
              { "number": 1, "kind": "Activation", "activation": { "errors": [] } },
              { "number": 2, "kind": "Activation", "activation": { "errors": ["Option '--server' is required."] } }
            ]
            """);
        var actual = (await response.ReadJsonAsync()).GetProperty("events");
        Assert.True(JsonElement.DeepEquals(expected.RootElement, actual), actual.GetRawText());
    }

    [Fact]
    public async Task GetEvents_AfterANumber_ReturnsTheEventsThatFollowItAndTheNumberToAskNext()
    {
        await using var api = await TestApi.StartAsync();
        api.Events.PublishActivation(ActivationRequest.Empty);
        api.Events.PublishActivation(ActivationRequest.Empty);
        api.Events.PublishActivation(ActivationRequest.Empty);

        using var response = await api.Client.GetAsync("/events?after=1", TestContext.Current.CancellationToken);

        var batch = await response.ReadJsonAsync();
        var events = batch.GetProperty("events").EnumerateArray();
        var numbers = events.Select(hostEvent => hostEvent.GetProperty("number").GetInt32());
        Assert.Equal([2, 3], numbers);
        Assert.Equal(3, batch.GetProperty("next").GetInt32());
    }

    [Fact]
    public async Task GetEvents_PageThatWaits_IsAnsweredByTheNextActivation()
    {
        await using var api = await TestApi.StartAsync();
        var waiting = api.Client.GetAsync("/events?after=0", TestContext.Current.CancellationToken);

        api.Events.PublishActivation(s_reportOpened);

        using var response = await waiting;
        var batch = await response.ReadJsonAsync();
        Assert.Equal(1, batch.GetProperty("events").GetArrayLength());
    }

    [Fact]
    public async Task GetEvents_HostThatStopsWhileThePageWaits_IsAnsweredWithNoEvent()
    {
        await using var api = await TestApi.StartAsync();
        var waiting = api.Client.GetAsync("/events?after=0", TestContext.Current.CancellationToken);

        await api.StopAsync();

        using var response = await waiting;
        var batch = await response.ReadJsonAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, batch.GetProperty("events").GetArrayLength());
    }
}
