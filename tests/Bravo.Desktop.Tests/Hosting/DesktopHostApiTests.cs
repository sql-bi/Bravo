using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Api.Security;
using Bravo.Api.Server;
using Bravo.Core.Activation;
using Bravo.Desktop.Activation;
using Bravo.Desktop.Diagnostics;
using Bravo.Desktop.Hosting;
using Bravo.Desktop.Tests.Shell;
using Bravo.Desktop.Tests.SingleInstance;
using Bravo.Platform.Windows.SingleInstance;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Bravo.Desktop.Tests.Hosting;

/// <summary>
/// The host as the application builds it, started: the API on a real socket, without a window.
/// </summary>
public sealed class DesktopHostApiTests : IAsyncLifetime
{
    private readonly OwnedInstance _instance = new();
    private readonly WebApplication _host;
    private readonly HttpClient _client = CreateClient();

    public DesktopHostApiTests()
    {
        var fatalExceptionHandler = Substitute.For<IFatalExceptionHandler>();
        var identity = BuildIdentity.Development("1.1.0");
        _host = DesktopHost.Build(identity, _instance.Ownership, new SingleInstanceOptions(), fatalExceptionHandler);
    }

    public async ValueTask InitializeAsync()
    {
        await _host.StartAsync(TestContext.Current.CancellationToken);

        var token = _host.Services.GetRequiredService<ApiToken>().Value;
        _client.BaseAddress = _host.Services.GetRequiredService<IApiAddressProvider>().GetAddress();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _host.DisposeAsync();
        _instance.Dispose();
    }

    [Fact]
    public async Task Start_Host_ServesTheApiOnTheLoopbackAddress()
    {
        using var response = await _client.GetAsync("/options", TestContext.Current.CancellationToken);

        Assert.Equal(IPAddress.Loopback.ToString(), _client.BaseAddress!.Host);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Start_Host_AnswersARequestWithoutTheTokenWith401()
    {
        using var anonymous = CreateClient();
        anonymous.BaseAddress = _client.BaseAddress;

        using var response = await anonymous.GetAsync("/options", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Activation_PostedWhileAPageWaits_ReachesThePageAsAnEvent()
    {
        using var window = new FakeMainWindow();
        using var windowClosed = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var pump = _host.Services.GetRequiredService<ActivationPump>().RunAsync(window, windowClosed.Token);
        var waiting = _client.GetAsync("/events", TestContext.Current.CancellationToken);

        var request = new ActivationRequest { ServerName = "localhost:12345", DatabaseName = "db" };
        _host.Services.GetRequiredService<IActivationChannel>().Post(request);

        using var response = await waiting;
        var batch = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        var activation = batch.GetProperty("events").EnumerateArray().Single().GetProperty("activation");
        Assert.Equal("localhost:12345", activation.GetProperty("serverName").GetString());
        Assert.Equal("db", activation.GetProperty("databaseName").GetString());

        await windowClosed.CancelAsync();
        await pump;
    }

    // A request that the host did not end would hold the stop for its whole limit, and then be cut off: the
    // page would get an error, not an answer.
    [Fact]
    public async Task Stop_WhileAPageWaitsForAnEvent_AnswersThePageWithNoEvent()
    {
        var listener = _host.Services.GetRequiredService<DiagnosticListener>();
        using var requestStarted = new RequestStartedSignal(listener, "/events");
        var waiting = _client.GetAsync("/events", TestContext.Current.CancellationToken);
        await requestStarted.Started.WaitAsync(TestContext.Current.CancellationToken);

        await _host.StopAsync(TestContext.Current.CancellationToken);

        using var response = await waiting;
        var batch = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, batch.GetProperty("events").GetArrayLength());
    }

    // No proxy: a request goes to the socket of the host, whatever the settings of the machine.
    private static HttpClient CreateClient()
    {
        return new HttpClient(new SocketsHttpHandler { UseProxy = false }, disposeHandler: true);
    }
}
