using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Bravo.Api.Events;
using Bravo.Api.Security;
using Bravo.Core.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Bravo.Api.Tests;

/// <summary>
/// The API as the application composes it, on a server in memory: the same services and the same pipeline,
/// with a clock that the test moves and one origin that a browser may call from.
/// </summary>
internal sealed class TestApi : IAsyncDisposable
{
    public const string AllowedOrigin = "https://ui.bravo.test";

    private readonly WebApplication _app;

    private TestApi(WebApplication app, FakeTimeProvider time)
    {
        _app = app;
        Time = time;

        // The name that the host filtering expects, and not the localhost of the test server.
        var server = app.GetTestServer();
        server.BaseAddress = new Uri($"http://{SecurityServiceCollectionExtensions.AllowedHost}/");

        Client = CreateAnonymousClient();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token.Value);
    }

    public FakeTimeProvider Time { get; }

    /// <summary>
    /// A client that sends the token of the API with every request.
    /// </summary>
    public HttpClient Client { get; }

    public ApiToken Token => _app.Services.GetRequiredService<ApiToken>();

    public IHostEventPublisher Events => _app.Services.GetRequiredService<IHostEventPublisher>();

    /// <param name="mapRoutes">Adds routes that exist only for a test, after the ones of the API.</param>
    public static async Task<TestApi> StartAsync(Action<IEndpointRouteBuilder>? mapRoutes = null)
    {
        var time = new FakeTimeProvider();
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<TimeProvider>(time);
        builder.Services.AddSingleton<IUserSettingsStore, InMemoryUserSettingsStore>();
        builder.Services.AddApi();
        builder.Services.Configure<ApiOptions>(options => options.AllowedOrigins.Add(AllowedOrigin));

        var app = builder.Build();
        app.UseApi();
        mapRoutes?.Invoke(app);
        await app.StartAsync();

        return new TestApi(app, time);
    }

    /// <summary>
    /// A client that sends no token.
    /// </summary>
    public HttpClient CreateAnonymousClient()
    {
        return _app.GetTestServer().CreateClient();
    }

    public Task StopAsync()
    {
        return _app.StopAsync();
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.DisposeAsync();
    }
}
