using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Xunit;

namespace Bravo.Api.Tests.Security;

/// <summary>
/// A browser lets a page read an answer only when the answer names the origin of the page: the API names the
/// origins it was given, and no other.
/// </summary>
public sealed class CorsTests
{
    private const string AllowOrigin = "Access-Control-Allow-Origin";

    [Fact]
    public async Task Preflight_FromAnAllowedOrigin_AllowsTheHeadersThatTheUserInterfaceSends()
    {
        await using var api = await TestApi.StartAsync();
        using var client = api.CreateAnonymousClient();
        using var request = CreatePreflight(TestApi.AllowedOrigin, HttpMethod.Put, "authorization,content-type");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(TestApi.AllowedOrigin, response.Headers.GetValues(AllowOrigin).Single());
        Assert.Equal("Authorization,Content-Type", response.Headers.GetValues("Access-Control-Allow-Headers").Single());
    }

    [Fact]
    public async Task Preflight_FromAnUnknownOrigin_AllowsNothing()
    {
        await using var api = await TestApi.StartAsync();
        using var client = api.CreateAnonymousClient();
        using var request = CreatePreflight("https://unknown.example", HttpMethod.Put, "authorization");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.False(response.Headers.Contains(AllowOrigin));
    }

    [Fact]
    public async Task Preflight_ForAHeaderThatIsNotNamed_AllowsOnlyTheNamedOnes()
    {
        await using var api = await TestApi.StartAsync();
        using var client = api.CreateAnonymousClient();
        using var request = CreatePreflight(TestApi.AllowedOrigin, HttpMethod.Get, "x-custom");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal("Authorization,Content-Type", response.Headers.GetValues("Access-Control-Allow-Headers").Single());
    }

    [Fact]
    public async Task Request_FromAnAllowedOrigin_IsAnsweredWithoutAllowingCredentials()
    {
        await using var api = await TestApi.StartAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/options");
        request.Headers.Add("Origin", TestApi.AllowedOrigin);

        using var response = await api.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(TestApi.AllowedOrigin, response.Headers.GetValues(AllowOrigin).Single());
        Assert.False(response.Headers.Contains("Access-Control-Allow-Credentials"));
    }

    [Fact]
    public async Task Request_FromAnUnknownOrigin_IsAnsweredWithoutNamingTheOrigin()
    {
        await using var api = await TestApi.StartAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/options");
        request.Headers.Add("Origin", "https://unknown.example");

        using var response = await api.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.False(response.Headers.Contains(AllowOrigin));
    }

    [Theory]
    [InlineData("/options")]
    [InlineData("/test/unexpected")]
    public async Task ErrorAnswer_ToAnAllowedOrigin_NamesTheOriginSoThatThePageCanReadTheProblem(string route)
    {
        await using var api = await TestApi.StartAsync(routes => routes.MapGet("/test/unexpected", ThrowUnexpected));
        using var client = api.CreateAnonymousClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, route);
        request.Headers.Add("Origin", TestApi.AllowedOrigin);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(TestApi.AllowedOrigin, response.Headers.GetValues(AllowOrigin).Single());
    }

    private static HttpRequestMessage CreatePreflight(string origin, HttpMethod method, string headers)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/options");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", method.Method);
        request.Headers.Add("Access-Control-Request-Headers", headers);
        return request;
    }

    private static string ThrowUnexpected()
    {
        throw new System.InvalidOperationException("Not foreseen.");
    }
}
