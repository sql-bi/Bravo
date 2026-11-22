using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Api.Security;
using Bravo.Api.Server;
using Bravo.Core.Activation;
using Bravo.Core.Errors;
using Bravo.Core.Policies;
using Bravo.Desktop.Activation;
using Bravo.Desktop.Development;
using Bravo.Desktop.Hosting;
using Bravo.Desktop.Tests.Shell;
using Bravo.Desktop.Tests.SingleInstance;
using Bravo.Platform.Windows.SingleInstance;
using Bravo.Platform.Windows.Tests;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bravo.Desktop.Tests.Hosting;

/// <summary>
/// The host as the application builds it, started: the API on a real socket, without a window.
/// </summary>
public sealed class DesktopHostApiTests : IAsyncLifetime
{
    // A development build without an update feed: its update check is a foreseen error.
    private const string ForeseenErrorRoute = "/releases/latest";
    private const string ForeseenErrorCategory = "Bravo.Api.Problems.BravoExceptionHandler";

    private readonly OwnedInstance _instance = new();
    private readonly TemporaryFolder _dataFolder = new();
    private readonly WebApplication _host;
    private readonly HttpClient _client = CreateClient();

    public DesktopHostApiTests()
    {
        var identity = BuildIdentity.Development("1.1.0");
        _host = DesktopHost.Build(identity, _dataFolder.AsDataFolder(), DevelopmentOptions.None, ApplicationPolicies.None, _instance.Ownership, new SingleInstanceOptions(), _dataFolder.Log);
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
        _dataFolder.Dispose();
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
    public async Task Start_Host_TellsTheVersionAndTheKindOfTheBuildItWasBuiltWith()
    {
        using var response = await _client.GetAsync("/application", TestContext.Current.CancellationToken);

        var application = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal("1.1.0", application.GetProperty("version").GetString());
        Assert.True(application.GetProperty("isDevelopmentBuild").GetBoolean());
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

    // The pane reads the log of the host through the API.
    [Fact]
    public async Task Error_ForeseenByAUseCase_IsReadFromTheLogUnderTheTraceOfItsAnswer()
    {
        using var response = await _client.GetAsync(ForeseenErrorRoute, TestContext.Current.CancellationToken);

        var traceId = await ReadTraceIdAsync(response);
        var log = await _client.GetFromJsonAsync<JsonElement>("/log", TestContext.Current.CancellationToken);
        var record = Assert.Single(
            log.GetProperty("records").EnumerateArray(),
            record => record.GetProperty("category").GetString() == ForeseenErrorCategory);
        Assert.Equal("Warning", record.GetProperty("level").GetString());
        Assert.Equal(traceId, record.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task Error_ForeseenByAUseCase_IsWrittenToTheFileUnderTheTraceOfItsAnswer()
    {
        using var response = await _client.GetAsync(ForeseenErrorRoute, TestContext.Current.CancellationToken);

        var traceId = await ReadTraceIdAsync(response);
        Assert.Contains($" Warning {traceId} {ForeseenErrorCategory}[1] ", ReadLogFile(), StringComparison.Ordinal);
    }

    // The requests are served after the start: none of them belongs to the trace of the start, or to the one
    // of another request.
    [Fact]
    public async Task Requests_TwoOfThem_HaveATraceEach()
    {
        using var first = await _client.GetAsync(ForeseenErrorRoute, TestContext.Current.CancellationToken);
        using var second = await _client.GetAsync(ForeseenErrorRoute, TestContext.Current.CancellationToken);

        Assert.NotEqual(await ReadTraceIdAsync(first), await ReadTraceIdAsync(second));
    }

    [Fact]
    public async Task Log_SecretsThatRequestsCarry_AreNeverRecorded()
    {
        const string Planted = "planted-secret-6f1c0b";
        using var anonymous = CreateClient();
        anonymous.BaseAddress = _client.BaseAddress;
        anonymous.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Planted);
        using var proxy = new StringContent($$"""{ "proxy": { "type": "Custom", "address": "http://user:{{Planted}}@proxy.test" } }""", Encoding.UTF8, "application/json");

        using var refused = await anonymous.GetAsync("/options", TestContext.Current.CancellationToken);
        using var failed = await _client.GetAsync($"{ForeseenErrorRoute}?password={Planted}", TestContext.Current.CancellationToken);
        using var saved = await _client.PutAsync("/options", proxy, TestContext.Current.CancellationToken);

        var token = _host.Services.GetRequiredService<ApiToken>().Value;
        var recorded = ReadLogFile() + string.Concat(_dataFolder.Log.Buffer.Read(after: 0).Records.Select(record => record.Message + record.Exception));
        Assert.Contains(nameof(ErrorCode.UpdateCheckFailed), recorded, StringComparison.Ordinal);
        Assert.DoesNotContain(Planted, recorded, StringComparison.Ordinal);
        Assert.DoesNotContain(token, recorded, StringComparison.Ordinal);
    }

    // The traceId of a problem is the identifier of its request in the W3C format: its middle part is the trace.
    private static async Task<string> ReadTraceIdAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        var traceId = problem.GetProperty("traceId").GetString()!;

        return traceId.Split('-')[1];
    }

    // The file stays open for writing while the host runs.
    private string ReadLogFile()
    {
        return OpenFile.ReadAllText(_dataFolder.Log.FilePath!);
    }

    // No proxy: a request goes to the socket of the host, whatever the settings of the machine.
    private static HttpClient CreateClient()
    {
        return new HttpClient(new SocketsHttpHandler { UseProxy = false }, disposeHandler: true);
    }
}
