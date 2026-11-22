using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Bravo.Api.Diagnostics;
using Bravo.Api.Events;
using Bravo.Api.Security;
using Bravo.Core.Application;
using Bravo.Core.Browser;
using Bravo.Core.Network;
using Bravo.Core.Policies;
using Bravo.Core.Updates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace Bravo.Api.Tests;

/// <summary>
/// The API as the application composes it, on a server in memory: the same services and the same pipeline,
/// with a clock that the test moves and one origin that a browser may call from.
/// </summary>
internal sealed class TestApi : IAsyncDisposable
{
    public const string AllowedOrigin = "https://ui.bravo.test";

    /// <summary>
    /// What the application of the tests says it is.
    /// </summary>
    public static ApplicationInfo Application { get; } = new()
    {
        Version = "1.2.3-test.4",
        Culture = CultureInfo.GetCultureInfo("it-IT"),
        IsDevelopmentBuild = true,
    };

    private readonly WebApplication _app;

    private TestApi(WebApplication app, FakeTimeProvider time, RecordingLoggerProvider logs)
    {
        _app = app;
        Time = time;
        Logs = logs;

        // The name that the host filtering expects, and not the localhost of the test server.
        var server = app.GetTestServer();
        server.BaseAddress = new Uri($"http://{SecurityServiceCollectionExtensions.AllowedHost}/");

        Client = CreateAnonymousClient();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token.Value);
    }

    public FakeTimeProvider Time { get; }

    /// <summary>
    /// What the API wrote to its log.
    /// </summary>
    public RecordingLoggerProvider Logs { get; }

    /// <summary>
    /// A client that sends the token of the API with every request.
    /// </summary>
    public HttpClient Client { get; }

    public ApiToken Token => _app.Services.GetRequiredService<ApiToken>();

    public IHostEventPublisher Events => _app.Services.GetRequiredService<IHostEventPublisher>();

    public LogBuffer LogBuffer => _app.Services.GetRequiredService<LogBuffer>();

    public IReleaseFeed ReleaseFeed => _app.Services.GetRequiredService<IReleaseFeed>();

    public IInstallationReader InstallationReader => _app.Services.GetRequiredService<IInstallationReader>();

    public IBrowserLauncher BrowserLauncher => _app.Services.GetRequiredService<IBrowserLauncher>();

    public IProxyCredentialDialog ProxyCredentialDialog => _app.Services.GetRequiredService<IProxyCredentialDialog>();

    public IProxyCredentialStore ProxyCredentialStore => _app.Services.GetRequiredService<IProxyCredentialStore>();

    public InMemoryUserSettingsStore Settings => _app.Services.GetRequiredService<InMemoryUserSettingsStore>();

    /// <param name="mapRoutes">Adds routes that exist only for a test, after the ones of the API.</param>
    /// <param name="policies">What an administrator set; none by default.</param>
    public static async Task<TestApi> StartAsync(
        Action<IEndpointRouteBuilder>? mapRoutes = null,
        ApplicationPolicies? policies = null)
    {
        var time = new FakeTimeProvider();
        var logs = new RecordingLoggerProvider();
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        builder.WebHost.UseTestServer();
        builder.Logging.AddProvider(logs);
        builder.Services.AddSingleton<TimeProvider>(time);
        builder.Services.AddUseCasesOfTheTests(policies);
        builder.Services.AddApi();
        builder.Services.Configure<ApiOptions>(options => options.AllowedOrigins.Add(AllowedOrigin));

        var app = builder.Build();
        app.UseApi();
        mapRoutes?.Invoke(app);
        await app.StartAsync();

        return new TestApi(app, time, logs);
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
