using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Bravo.Api.Problems;
using Xunit;

namespace Bravo.Api.Tests.Options;

public sealed class OptionsEndpointsTests
{
    private const string Route = "/options";

    private const string Defaults = """
        {
          "theme": "Auto",
          "telemetryEnabled": true,
          "customTemplatesEnabled": true,
          "updateCheckEnabled": true,
          "useSystemBrowserForAuthentication": false,
          "diagnosticLevel": "None",
          "proxy": { "type": "System", "useDefaultCredentials": true, "bypassOnLocal": true }
        }
        """;

    private const string Changed = """
        {
          "theme": "Dark",
          "telemetryEnabled": false,
          "customTemplatesEnabled": false,
          "updateCheckEnabled": false,
          "useSystemBrowserForAuthentication": true,
          "diagnosticLevel": "Verbose",
          "proxy": {
            "type": "Custom",
            "useDefaultCredentials": false,
            "address": "proxy.example:8080",
            "bypassOnLocal": false,
            "bypassList": "^intranet\\.;\\.local$"
          },
          "customOptions": { "sidebarCollapsed": true, "sizes": { "main": [70, 30] }, "locale": "it" }
        }
        """;

    [Fact]
    public async Task GetOptions_NothingChosenYet_ReturnsTheDefaultsWithNoMemberThatHasNoValue()
    {
        await using var api = await TestApi.StartAsync();

        using var response = await api.Client.GetAsync(Route, TestContext.Current.CancellationToken);

        AssertJson(Defaults, await response.ReadJsonAsync());
    }

    [Fact]
    public async Task UpdateOptions_EveryOptionChanged_ReturnsTheOptionsAsTheyWereKept()
    {
        await using var api = await TestApi.StartAsync();

        using var response = await PutAsync(api, JsonNode.Parse(Changed)!);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertJson(Changed, await response.ReadJsonAsync());
    }

    [Fact]
    public async Task GetOptions_AfterAnUpdate_ReturnsWhatTheUpdateSent()
    {
        await using var api = await TestApi.StartAsync();
        using var update = await PutAsync(api, JsonNode.Parse(Changed)!);

        using var response = await api.Client.GetAsync(Route, TestContext.Current.CancellationToken);

        AssertJson(Changed, await response.ReadJsonAsync());
    }

    [Theory]
    [InlineData("address")]
    [InlineData("bypassList")]
    public async Task UpdateOptions_ProxyTextThatIsBlank_KeepsTheMemberWithoutAValue(string member)
    {
        await using var api = await TestApi.StartAsync();
        var body = JsonNode.Parse(Changed)!;
        body["proxy"]![member] = "  ";

        using var response = await PutAsync(api, body);

        var proxy = (await response.ReadJsonAsync()).GetProperty("proxy");
        Assert.False(proxy.TryGetProperty(member, out _));
    }

    [Theory]
    [InlineData("address", null)]
    [InlineData("bypassList", null)]
    public async Task UpdateOptions_NullForAnOptionalMember_IsReadAsAMemberThatIsAbsent(string member, string? value)
    {
        await using var api = await TestApi.StartAsync();
        var body = JsonNode.Parse(Changed)!;
        body["proxy"]![member] = value;

        using var response = await PutAsync(api, body);

        var proxy = (await response.ReadJsonAsync()).GetProperty("proxy");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(proxy.TryGetProperty(member, out _));
    }

    [Theory]
    [InlineData("address", "http://exa mple")]
    [InlineData("bypassList", "intranet;(unclosed")]
    public async Task UpdateOptions_ProxyTextThatIsNotValid_IsAnswered400WithTheMember(string member, string value)
    {
        await using var api = await TestApi.StartAsync();
        var body = JsonNode.Parse(Changed)!;
        body["proxy"]![member] = value;

        using var response = await PutAsync(api, body);

        var problem = await response.ReadJsonAsync();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(nameof(ApiProblemCode.ValidationFailed), problem.GetProperty("code").GetString());
        Assert.True(problem.GetProperty("errors").TryGetProperty($"proxy.{member}", out _), problem.GetRawText());
    }

    [Fact]
    public async Task UpdateOptions_OptionsThatAreNotValid_KeepsTheOptionsAsTheyWere()
    {
        await using var api = await TestApi.StartAsync();
        var body = JsonNode.Parse(Changed)!;
        body["proxy"]!["bypassList"] = "(unclosed";
        using var update = await PutAsync(api, body);

        using var response = await api.Client.GetAsync(Route, TestContext.Current.CancellationToken);

        AssertJson(Defaults, await response.ReadJsonAsync());
    }

    [Fact]
    public async Task UpdateOptions_EnumSentAsANumber_IsAnswered400()
    {
        await using var api = await TestApi.StartAsync();
        var body = JsonNode.Parse(Changed)!;
        body["theme"] = 2;

        using var response = await PutAsync(api, body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(nameof(ApiProblemCode.InvalidRequest), await response.ReadProblemCodeAsync());
    }

    [Fact]
    public async Task UpdateOptions_BooleanSentAsText_IsAnswered400()
    {
        await using var api = await TestApi.StartAsync();
        var body = JsonNode.Parse(Changed)!;
        body["telemetryEnabled"] = "true";

        using var response = await PutAsync(api, body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("theme")]
    [InlineData("telemetryEnabled")]
    [InlineData("proxy")]
    public async Task UpdateOptions_MemberThatAlwaysHasAValueIsMissing_IsAnswered400(string member)
    {
        await using var api = await TestApi.StartAsync();
        var body = JsonNode.Parse(Changed)!.AsObject();
        body.Remove(member);

        using var response = await PutAsync(api, body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(nameof(ApiProblemCode.InvalidRequest), await response.ReadProblemCodeAsync());
    }

    [Fact]
    public async Task UpdateOptions_NullForAMemberThatAlwaysHasAValue_IsAnswered400()
    {
        await using var api = await TestApi.StartAsync();
        var body = JsonNode.Parse(Changed)!;
        body["proxy"] = null;

        using var response = await PutAsync(api, body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateOptions_MemberThatTheHostDoesNotKnow_IsIgnored()
    {
        await using var api = await TestApi.StartAsync();
        var body = JsonNode.Parse(Changed)!;
        body["addedByALaterUserInterface"] = true;

        using var response = await PutAsync(api, body);

        AssertJson(Changed, await response.ReadJsonAsync());
    }

    private static Task<HttpResponseMessage> PutAsync(TestApi api, JsonNode body)
    {
        return api.Client.PutAsJsonAsync(Route, body, TestContext.Current.CancellationToken);
    }

    private static void AssertJson(string expected, JsonElement actual)
    {
        using var expectedDocument = JsonDocument.Parse(expected);
        Assert.True(
            JsonElement.DeepEquals(expectedDocument.RootElement, actual),
            $"Expected: {expectedDocument.RootElement.GetRawText()}\nActual: {actual.GetRawText()}");
    }
}
