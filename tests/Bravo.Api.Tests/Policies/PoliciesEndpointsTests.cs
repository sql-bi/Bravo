using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Bravo.Core.Policies;
using Xunit;

namespace Bravo.Api.Tests.Policies;

public sealed class PoliciesEndpointsTests
{
    private const string Route = "/policies";

    [Fact]
    public async Task GetPolicies_EveryPolicyConfigured_ReturnsTheirValues()
    {
        var policies = new ApplicationPolicies
        {
            TelemetryEnabled = false,
            UpdateCheckEnabled = false,
            UseSystemBrowserForAuthentication = true,
            BuiltInTemplatesEnabled = false,
            CustomTemplatesEnabled = true,
            CustomTemplatesOrganizationRepositoryPath = @"\\contoso\templates\%USERNAME%",
        };
        await using var api = await TestApi.StartAsync(policies: policies);

        using var response = await api.Client.GetAsync(Route, TestContext.Current.CancellationToken);

        var expected = """
            {
              "telemetryEnabled": false,
              "updateCheckEnabled": false,
              "useSystemBrowserForAuthentication": true,
              "builtInTemplatesEnabled": false,
              "customTemplatesEnabled": true,
              "customTemplatesOrganizationRepositoryPath": "\\\\contoso\\templates\\%USERNAME%"
            }
            """;
        var actual = await response.ReadJsonAsync();
        using var expectedDocument = JsonDocument.Parse(expected);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(JsonElement.DeepEquals(expectedDocument.RootElement, actual), actual.GetRawText());
    }

    // A policy that is not configured travels as an absent member, never as null.
    [Fact]
    public async Task GetPolicies_OnePolicyConfigured_ReturnsItAlone()
    {
        var policies = new ApplicationPolicies { UpdateCheckEnabled = true };
        await using var api = await TestApi.StartAsync(policies: policies);

        using var response = await api.Client.GetAsync(Route, TestContext.Current.CancellationToken);

        var actual = await response.ReadJsonAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("""{"updateCheckEnabled":true}""", actual.GetRawText());
    }

    [Fact]
    public async Task GetPolicies_NoPolicy_ReturnsAnEmptyObject()
    {
        await using var api = await TestApi.StartAsync();

        using var response = await api.Client.GetAsync(Route, TestContext.Current.CancellationToken);

        var actual = await response.ReadJsonAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("{}", actual.GetRawText());
    }

    [Fact]
    public async Task GetPolicies_WithoutTheToken_IsAnswered401()
    {
        await using var api = await TestApi.StartAsync();
        using var client = api.CreateAnonymousClient();

        using var response = await client.GetAsync(Route, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
