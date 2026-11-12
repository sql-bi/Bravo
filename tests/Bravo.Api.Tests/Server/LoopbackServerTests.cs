using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Bravo.Api.Security;
using Bravo.Api.Server;
using Bravo.Core.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bravo.Api.Tests.Server;

/// <summary>
/// The API on a real socket: Kestrel, the loopback address, a port that the system chooses.
/// </summary>
public sealed class LoopbackServerTests
{
    [Fact]
    public async Task GetAddress_AfterTheStart_IsTheLoopbackAddressWithThePortOfTheSystem()
    {
        await using var app = BuildApplication();
        await app.StartAsync(TestContext.Current.CancellationToken);

        var address = app.Services.GetRequiredService<IApiAddressProvider>().GetAddress();

        Assert.Equal(Uri.UriSchemeHttp, address.Scheme);
        Assert.Equal(IPAddress.Loopback.ToString(), address.Host);
        Assert.NotEqual(0, address.Port);
    }

    [Fact]
    public async Task GetAddress_BeforeTheStart_Throws()
    {
        await using var app = BuildApplication();

        var provider = app.Services.GetRequiredService<IApiAddressProvider>();

        Assert.Throws<InvalidOperationException>(provider.GetAddress);
    }

    [Fact]
    public async Task Request_ToTheAddressWithTheToken_IsAnsweredWithoutNamingTheServer()
    {
        await using var app = BuildApplication();
        await app.StartAsync(TestContext.Current.CancellationToken);
        using var client = CreateClient(app);

        using var response = await client.GetAsync("/options", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(response.Headers.Server);
    }

    [Fact]
    public async Task Request_ToTheAddressWithoutAToken_IsAnswered401()
    {
        await using var app = BuildApplication();
        await app.StartAsync(TestContext.Current.CancellationToken);
        using var client = CreateClient(app);
        client.DefaultRequestHeaders.Authorization = null;

        using var response = await client.GetAsync("/options", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static WebApplication BuildApplication()
    {
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        builder.WebHost.UseLoopbackServer();
        builder.Services.AddSingleton<IUserSettingsStore, InMemoryUserSettingsStore>();
        builder.Services.AddApi();

        var app = builder.Build();
        app.UseApi();
        return app;
    }

    // No proxy: the request goes to the socket of the test, whatever the settings of the machine.
    private static HttpClient CreateClient(WebApplication app)
    {
        var handler = new SocketsHttpHandler { UseProxy = false };
        var client = new HttpClient(handler, disposeHandler: true)
        {
            BaseAddress = app.Services.GetRequiredService<IApiAddressProvider>().GetAddress(),
        };
        var token = app.Services.GetRequiredService<ApiToken>().Value;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
