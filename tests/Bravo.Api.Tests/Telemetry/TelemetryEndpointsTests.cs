using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Bravo.Core.Telemetry;
using Xunit;

namespace Bravo.Api.Tests.Telemetry;

public sealed class TelemetryEndpointsTests
{
    private const string Route = "/telemetry";

    [Fact]
    public async Task GetTelemetry_BuildThatSendsIt_ReturnsWhereAndWhatItsItemsCarry()
    {
        var telemetry = new PageTelemetry
        {
            ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=http://127.0.0.1:9",
            UserId = "user",
            SessionId = "session",
            Version = "1.2.3-test.4",
            Properties = new Dictionary<string, string> { ["ProductName"] = "Bravo", ["InstallScope"] = "PerUser" },
        };
        await using var api = await TestApi.StartAsync(telemetry: telemetry);

        using var response = await api.Client.GetAsync(Route, TestContext.Current.CancellationToken);

        var expected = """
            {
              "connectionString": "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=http://127.0.0.1:9",
              "userId": "user",
              "sessionId": "session",
              "version": "1.2.3-test.4",
              "properties": { "ProductName": "Bravo", "InstallScope": "PerUser" }
            }
            """;
        var actual = await response.ReadJsonAsync();
        using var expectedDocument = JsonDocument.Parse(expected);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(JsonElement.DeepEquals(expectedDocument.RootElement, actual), actual.GetRawText());
    }

    // The page creates no sender without a connection string: it travels as an absent member, never as null.
    [Fact]
    public async Task GetTelemetry_BuildThatSendsNothing_ReturnsNoConnectionString()
    {
        await using var api = await TestApi.StartAsync();

        using var response = await api.Client.GetAsync(Route, TestContext.Current.CancellationToken);

        var actual = await response.ReadJsonAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(actual.TryGetProperty("connectionString", out _), actual.GetRawText());
        Assert.Equal(TestApi.PageTelemetry.SessionId, actual.GetProperty("sessionId").GetString());
    }

    [Fact]
    public async Task GetTelemetry_WithoutTheToken_IsAnswered401()
    {
        await using var api = await TestApi.StartAsync();
        using var client = api.CreateAnonymousClient();

        using var response = await client.GetAsync(Route, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
