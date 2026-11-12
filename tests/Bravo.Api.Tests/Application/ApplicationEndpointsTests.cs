using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Bravo.Api.Tests.Application;

public sealed class ApplicationEndpointsTests
{
    private const string Route = "/application";

    private const string Expected = """
        {
          "version": "1.2.3-test.4",
          "culture": { "ietfLanguageTag": "it-IT", "twoLetterISOLanguageName": "it" },
          "isDevelopmentBuild": true
        }
        """;

    [Fact]
    public async Task GetApplication_ApplicationOfTheTests_ReturnsItsVersionItsCultureAndTheKindOfItsBuild()
    {
        await using var api = await TestApi.StartAsync();

        using var response = await api.Client.GetAsync(Route, TestContext.Current.CancellationToken);

        var actual = await response.ReadJsonAsync();
        using var expected = JsonDocument.Parse(Expected);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(JsonElement.DeepEquals(expected.RootElement, actual), actual.GetRawText());
    }

    [Fact]
    public async Task GetApplication_WithoutTheToken_IsAnswered401()
    {
        await using var api = await TestApi.StartAsync();
        using var client = api.CreateAnonymousClient();

        using var response = await client.GetAsync(Route, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
