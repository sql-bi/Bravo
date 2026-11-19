using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Bravo.Api.Problems;
using NSubstitute;
using Xunit;

namespace Bravo.Api.Tests.Browser;

public sealed class BrowserEndpointsTests
{
    private const string Route = "/browser/open";

    [Fact]
    public async Task OpenInBrowser_PageOfATrustedSite_OpensItAndAnswers204()
    {
        await using var api = await TestApi.StartAsync();
        var address = "https://docs.sqlbi.com/bravo/features/format-dax";

        using var response = await PostAsync(api, $$"""{ "address": "{{address}}" }""");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        api.BrowserLauncher.Received(1).Open(new Uri(address));
    }

    [Theory]
    [InlineData("https://attacker.test/")]
    [InlineData("file:///C:/Windows/System32/calc.exe")]
    [InlineData("calc.exe")]
    [InlineData("")]
    public async Task OpenInBrowser_AnyOtherAddress_IsAnswered400WithTheMemberAndOpensNothing(string address)
    {
        await using var api = await TestApi.StartAsync();

        using var response = await PostAsync(api, $$"""{ "address": "{{address}}" }""");

        var problem = await response.ReadJsonAsync();
        Assert.Equal(nameof(ApiProblemCode.ValidationFailed), problem.GetProperty("code").GetString());
        Assert.Equal(JsonValueKind.Array, problem.GetProperty("errors").GetProperty("address").ValueKind);
        api.BrowserLauncher.DidNotReceiveWithAnyArgs().Open(default!);
    }

    [Fact]
    public async Task OpenInBrowser_NoAddress_IsAnswered400AndOpensNothing()
    {
        await using var api = await TestApi.StartAsync();

        using var response = await PostAsync(api, "{}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        api.BrowserLauncher.DidNotReceiveWithAnyArgs().Open(default!);
    }

    private static async Task<HttpResponseMessage> PostAsync(TestApi api, string body)
    {
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        return await api.Client.PostAsync(Route, content, TestContext.Current.CancellationToken);
    }
}
