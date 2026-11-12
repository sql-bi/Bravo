using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Bravo.Api.Problems;
using Bravo.Api.Security;
using Microsoft.AspNetCore.Builder;
using Xunit;

namespace Bravo.Api.Tests.Security;

public sealed class ApiTokenAuthenticationHandlerTests
{
    private const string Route = "/options";

    [Fact]
    public async Task Request_WithTheToken_IsAnswered()
    {
        await using var api = await TestApi.StartAsync();

        using var response = await api.Client.GetAsync(Route, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithoutAToken_IsAnswered401WithItsCode()
    {
        await using var api = await TestApi.StartAsync();
        using var client = api.CreateAnonymousClient();

        using var response = await client.GetAsync(Route, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Bearer", response.Headers.WwwAuthenticate.ToString());
        Assert.Equal(ProblemCodes.Unauthorized, await response.ReadProblemCodeAsync());
    }

    [Fact]
    public async Task Request_WithTheTokenOfAnotherProcess_IsAnswered401()
    {
        await using var api = await TestApi.StartAsync();
        using var client = api.CreateAnonymousClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new ApiToken().Value);

        using var response = await client.GetAsync(Route, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("{0}")]
    [InlineData("Basic {0}")]
    [InlineData("Bearer")]
    [InlineData("Bearer {0} {0}")]
    public async Task Request_WithTheTokenInAnotherForm_IsAnswered401(string format)
    {
        await using var api = await TestApi.StartAsync();
        using var client = api.CreateAnonymousClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, Route);
        request.Headers.TryAddWithoutValidation("Authorization", string.Format(null, format, api.Token.Value));

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithTheSchemeInAnotherCase_IsAnswered()
    {
        await using var api = await TestApi.StartAsync();
        using var client = api.CreateAnonymousClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", api.Token.Value);

        using var response = await client.GetAsync(Route, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Request_ToAPathThatMatchesNoRouteWithoutAToken_IsAnswered401AndNot404()
    {
        await using var api = await TestApi.StartAsync();
        using var client = api.CreateAnonymousClient();

        using var response = await client.GetAsync("/no-such-route", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(ProblemCodes.Unauthorized, await response.ReadProblemCodeAsync());
    }

    [Fact]
    public async Task Request_ToARouteAddedWithoutAskingForTheToken_IsAnswered401()
    {
        await using var api = await TestApi.StartAsync(routes => routes.MapGet("/added-later", () => "content"));
        using var client = api.CreateAnonymousClient();

        using var response = await client.GetAsync("/added-later", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
