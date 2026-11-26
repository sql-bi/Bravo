using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Bravo.Api.Diagnostics;
using Bravo.Api.Security;
using Bravo.Api.Server;
using Bravo.Core.Policies;
using Bravo.Core.Settings;
using Bravo.Core.Telemetry;
using Bravo.Desktop.Development;
using Bravo.Desktop.Hosting;
using Bravo.Desktop.Telemetry;
using Bravo.Desktop.Tests.Hosting;
using Bravo.Desktop.Tests.SingleInstance;
using Bravo.Desktop.WebView;
using Bravo.Platform.Windows.Network;
using Bravo.Platform.Windows.SingleInstance;
using Bravo.Platform.Windows.Tests;
using Bravo.Platform.Windows.Tests.Network;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Web.WebView2.Core;
using NSubstitute;
using Xunit;

namespace Bravo.Desktop.Tests.WebView;

/// <summary>
/// The webview with the real engine, in a window on a thread with a message loop, on a page of the test.
/// The API is the one of the host as the application builds it, on a real socket.
/// </summary>
public sealed class WebViewHostTests : IAsyncLifetime
{
    private const string PrefersDarkScript = "matchMedia('(prefers-color-scheme: dark)').matches";

    // The page posts to the endpoint of its telemetry, as the SDK does, and says in the title of its document
    // how the request ended.
    private const string PostToIngestionScript = """
        fetch("https://ingestion.bravo.test/v2/track", { method: "POST", body: "[]" })
          .then(response => "status " + response.status, () => "failed")
          .then(result => { document.title = "ingestion:" + JSON.stringify({ result }); });
        """;

    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(60);

    // A name that no server has: only the proxy of a test answers for it.
    private static readonly Uri s_ingestion = new("https://ingestion.bravo.test/");

    private readonly OwnedInstance _instance = new();
    private readonly TemporaryFolder _dataFolder = new();
    private readonly WebViewTestSite _site = new();
    private readonly WebViewTestWindow _window = new();
    private readonly WebApplication _host;
    private readonly RequestLog _requests;
    private WebViewHost _webView;
    private CoreWebView2Controller? _holder;

    public WebViewHostTests()
    {
        var identity = BuildIdentity.Development("1.1.0");
        _host = DesktopHost.Build(identity, _dataFolder.AsDataFolder(), DevelopmentOptions.None, ApplicationPolicies.None, _instance.Ownership, new SingleInstanceOptions(), _dataFolder.Log, TelemetrySession.Start());
        _requests = new RequestLog(_host.Services.GetRequiredService<DiagnosticListener>());

        _webView = CreateWebView(_site.Options);
    }

    public async ValueTask InitializeAsync()
    {
        await _host.StartAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _window.InvokeAsync(_ => CloseAsync(_webView));
        await ReleaseDataFolderAsync();
        await _window.DisposeAsync();
        _requests.Dispose();
        await _host.DisposeAsync();
        _instance.Dispose();
        _site.Dispose();
        _dataFolder.Dispose();
    }

    [Fact]
    public async Task Start_Page_IsServedFromTheContentFolderUnderTheOriginOfThePage()
    {
        await StartAndReadReportAsync();

        var source = await _window.InvokeAsync(_ => Task.FromResult(_webView.Engine!.Source));
        Assert.Equal("https://bravo.invalid/index.html", source);
    }

    [Fact]
    public async Task Start_Engine_IsRecordedWithTheVersionOfItsRuntimeUnderATrace()
    {
        await StartAndReadReportAsync();

        var version = await _window.InvokeAsync(_ => Task.FromResult(_webView.Engine!.Environment.BrowserVersionString));
        var record = Assert.Single(WebViewHostRecords());
        Assert.Contains(version, record.Message, StringComparison.Ordinal);
        Assert.NotNull(record.TraceId);
    }

    [Fact]
    public async Task Start_Page_FindsTheAddressAndTheTokenOfTheApiBeforeItsOwnScriptRuns()
    {
        var report = await StartAndReadReportAsync();

        var address = _host.Services.GetRequiredService<IApiAddressProvider>().GetAddress();
        var connection = report.GetProperty("connection");
        Assert.Equal(address.GetLeftPart(UriPartial.Authority), connection.GetProperty("address").GetString());
        Assert.Equal(_host.Services.GetRequiredService<ApiToken>().Value, connection.GetProperty("token").GetString());
    }

    [Fact]
    public async Task Start_PageThatTriesToReplaceTheConnection_KeepsTheOneOfTheHost()
    {
        var report = await StartAndReadReportAsync();

        Assert.True(report.GetProperty("unchanged").GetBoolean());
    }

    [Fact]
    public async Task Start_DocumentOfAnotherOrigin_DoesNotFindTheConnection()
    {
        var report = await StartAndReadReportAsync();

        Assert.Equal("undefined in null", report.GetProperty("otherOrigin").GetString());
    }

    [Fact]
    public async Task Start_PageThatCallsTheApiWithTheToken_IsAnsweredThroughAPreflightRequest()
    {
        var report = await StartAndReadReportAsync();

        await AssertTheApiAnsweredAsync(report);
        Assert.Equal(["OPTIONS /options", "GET /options"], _requests.Requests);
    }

    [Fact]
    public async Task Start_Settings_TurnOffWhatThePageDoesNotUse()
    {
        await StartAndReadReportAsync();

        var settings = await _window.InvokeAsync(_ => Task.FromResult(_webView.Engine!.Settings));
        var enabled = await _window.InvokeAsync(_ => Task.FromResult(new[]
        {
            settings.AreHostObjectsAllowed,
            settings.IsWebMessageEnabled,
            settings.AreDefaultScriptDialogsEnabled,
            settings.AreDefaultContextMenusEnabled,
            settings.AreBrowserAcceleratorKeysEnabled,
            settings.AreDevToolsEnabled,
            settings.IsStatusBarEnabled,
            settings.IsPasswordAutosaveEnabled,
            settings.IsGeneralAutofillEnabled,
            settings.IsPinchZoomEnabled,
            settings.IsSwipeNavigationEnabled,
            settings.IsReputationCheckingRequired,
        }));
        Assert.All(enabled, Assert.False);
    }

    [Fact]
    public async Task Navigate_OutsideTheOriginOfThePage_IsCancelledAndThePageStays()
    {
        await StartAndReadReportAsync();

        var (isSuccess, status, source) = await _window.InvokeAsync(async _ =>
        {
            var engine = _webView.Engine!;
            var completed = new TaskCompletionSource<CoreWebView2NavigationCompletedEventArgs>();
            engine.NavigationCompleted += (_, e) => completed.TrySetResult(e);
            engine.Navigate("https://example.com/");
            var result = await completed.Task.WaitAsync(s_timeout);
            return (result.IsSuccess, result.WebErrorStatus, engine.Source);
        });

        Assert.False(isSuccess);
        Assert.Equal(CoreWebView2WebErrorStatus.OperationCanceled, status);
        Assert.Equal("https://bravo.invalid/index.html", source);
    }

    [Fact]
    public async Task OpenWindow_FromThePage_OpensNoWindow()
    {
        await StartAndReadReportAsync();

        var opened = await _window.InvokeAsync(_ =>
            _webView.Engine!.ExecuteScriptAsync("String(window.open('https://example.com/'))"));

        Assert.Equal("\"null\"", opened);
    }

    // The page asks to show notifications, which it never does, and says in the title of its document what
    // it was answered and what the permission reads afterwards. Without the answer of the application the
    // engine would ask the user, and the page would wait.
    [Fact]
    public async Task Page_ThatAsksForAPermissionItDoesNotUse_IsDeniedAndTheAnswerIsNotKept()
    {
        await StartAndReadReportAsync();

        var permission = await RunInPageAsync("permission:", """
            (async () => {
              const answer = await Notification.requestPermission();
              const after = (await navigator.permissions.query({ name: "notifications" })).state;
              document.title = "permission:" + JSON.stringify({ answer, after });
            })();
            """);

        Assert.Equal("denied", permission.GetProperty("answer").GetString());
        Assert.Equal("prompt", permission.GetProperty("after").GetString());
    }

    // The machine that runs the tests is in one of the two color modes: with both themes, one of the two
    // runs asks the engine for the scheme that the system does not have.
    [Theory]
    [InlineData(Theme.Dark, "true")]
    [InlineData(Theme.Light, "false")]
    public async Task Start_ThemeOfTheUser_IsTheColorSchemeThatThePageIsTold(Theme theme, string prefersDark)
    {
        await _window.InvokeAsync(_ => _webView.ApplyTheme(theme, Color.Black));

        await StartAndReadReportAsync();

        var answer = await _window.InvokeAsync(_ => _webView.Engine!.ExecuteScriptAsync(PrefersDarkScript));
        Assert.Equal(prefersDark, answer);
    }

    // The page listens to the change of the color scheme, as the user interface does, and says in the title
    // of its document what it was told.
    [Fact]
    public async Task ApplyTheme_AfterThePageHasLoaded_TellsThePageTheNewColorScheme()
    {
        await _window.InvokeAsync(_ => _webView.ApplyTheme(Theme.Light, Color.White));
        await StartAndReadReportAsync();

        var change = await RunInPageAsync(
            "scheme:",
            """
            matchMedia("(prefers-color-scheme: dark)").addEventListener("change", event => {
              document.title = "scheme:" + JSON.stringify({ dark: event.matches });
            });
            """,
            () => _webView.ApplyTheme(Theme.Dark, Color.Black));

        Assert.True(change.GetProperty("dark").GetBoolean());
    }

    [Fact]
    public async Task RenderProcessThatExits_IsFollowedByAReloadOfThePage()
    {
        await StartAndReadReportAsync();

        var reloaded = WaitForReportAsync((_, report) => report.GetProperty("loads").GetInt32() == 2);
        await _window.InvokeAsync(form =>
        {
            // The protocol of the developer tools ends the process as a crash does. Nothing answers the call.
            _ = _webView.Engine!.CallDevToolsProtocolMethodAsync("Page.crash", "{}");
        });

        var report = await reloaded;
        await AssertTheApiAnsweredAsync(report);
        var records = WebViewHostRecords().Skip(1).Select(record => (record.Level, record.EventId)).ToList();

        // The failure of the process, then the recovery.
        Assert.Equal([(LogRecordLevel.Warning, 2), (LogRecordLevel.Information, 3)], records);
    }

    [Fact]
    public async Task BrowserProcessThatExits_IsFollowedByANewControlWithThePage()
    {
        await StartAndReadReportAsync();
        var first = await _window.InvokeAsync(_ => Task.FromResult(_webView.Engine!));

        var recreated = WaitForReportAsync((engine, _) => !ReferenceEquals(engine, first));
        var browserProcessId = await _window.InvokeAsync(_ => Task.FromResult((int)first.BrowserProcessId));
        using (var browser = Process.GetProcessById(browserProcessId))
        {
            browser.Kill();
        }

        var report = await recreated;
        await AssertTheApiAnsweredAsync(report);
    }

    // The API on the loopback address does not go through the proxy: the page found it, and the proxy was not
    // asked for it.
    [Fact]
    public async Task Page_ProxyServerOfTheUser_TakesTheRequestsToTheEndpointOfTheTelemetry()
    {
        await using var proxy = new TestProxy();
        UseProxy(proxy, new CredentialCache());
        var report = await StartAndReadReportAsync();

        await RunInPageAsync("ingestion:", PostToIngestionScript);

        await AssertTheApiAnsweredAsync(report);
        Assert.Equal("CONNECT", IngestionRequests(proxy).First().Method);
        Assert.DoesNotContain(proxy.Requests, request => request.Target.Contains("127.0.0.1", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Page_ProxyServerThatAsksForBasicCredentials_IsGivenTheOnesThatTheUserEntered()
    {
        var credentials = new NetworkCredential("test-user", "test-password");
        await using var proxy = new TestProxy { Challenges = ["Basic realm=\"test\""], Accepts = credentials };
        UseProxy(proxy, credentials);
        await StartAndReadReportAsync();

        await RunInPageAsync("ingestion:", PostToIngestionScript);

        Assert.Contains(IngestionRequests(proxy), request => request.AuthorizationScheme == "Basic");
        Assert.DoesNotContain(WebViewHostRecords(), record => record.EventId == 5);
    }

    // The window of the engine would hold the request until the user answered it: the request ends at once.
    [Fact]
    public async Task Page_ProxyServerThatAsksForCredentialsThatTheUserDidNotEnter_IsRefusedWithoutAWindowAndSaysSo()
    {
        await using var proxy = new TestProxy { Challenges = ["Basic realm=\"test\""] };
        UseProxy(proxy, new CredentialCache());
        await StartAndReadReportAsync();

        var posted = await RunInPageAsync("ingestion:", PostToIngestionScript);

        Assert.Equal("failed", posted.GetProperty("result").GetString());
        Assert.All(IngestionRequests(proxy), request => Assert.Null(request.AuthorizationScheme));
        var record = Assert.Single(WebViewHostRecords(), record => record.EventId == 5);
        Assert.Equal(LogRecordLevel.Warning, record.Level);
        Assert.Contains("127.0.0.1", record.Message, StringComparison.Ordinal);
    }

    // The browser process of an earlier run, started with another proxy, still holds the data folder: the
    // webview is created again once it has ended.
    [Fact]
    public async Task Start_DataFolderHeldByABrowserProcessWithOtherArguments_CreatesTheWebViewOnceItIsReleased()
    {
        var time = new FakeTimeProvider();
        var logger = new RecordingLogger<WebViewHost>();
        _webView.Dispose();
        _webView = CreateWebView(_site.Options, timeProvider: time, logger: logger);
        await HoldDataFolderAsync();

        var report = WaitForReportAsync((_, _) => true);
        await _window.InvokeAsync(form => _webView.Start(form));
        await logger.WaitForEntryAsync(6).WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        await ReleaseDataFolderAsync();

        // The release went through the thread of the webview after the record: the wait had begun.
        time.Advance(TimeSpan.FromSeconds(1));

        await AssertTheApiAnsweredAsync(await report, logger.Entries.Select(entry => $"{entry.Level} {entry.Id.Id}"));
        Assert.Equal([6, 1], logger.Entries.Select(entry => entry.Id.Id));
    }

    [Fact]
    public async Task Start_ContentFolderWithoutThePage_EndsWithTheCodeOfAMissingUserInterface()
    {
        using var site = new WebViewTestSite(withPage: false);
        using var webView = CreateWebView(site.Options);

        var ended = await StartUntilEndedAsync(webView);

        Assert.Equal(WebViewFailure.ContentMissing, ended.Failure);
        Assert.Equal(ExitCode.UserInterfaceMissing, ended.ExitCode);
        var record = Assert.Single(WebViewHostRecords());
        Assert.Equal((LogRecordLevel.Error, 4), (record.Level, record.EventId));
    }

    // The data folder is still held when the wait is over: the engine cannot be created. An exception of the
    // engine that ends the application is one that nothing handled.
    [Fact]
    public async Task Start_DataFolderHeldPastTheWait_EndsAndSendsTheExceptionToTheTelemetry()
    {
        // Every reading moves the clock a minute: the wait is over at the first failure.
        var time = new FakeTimeProvider { AutoAdvanceAmount = TimeSpan.FromMinutes(1) };
        var telemetry = Substitute.For<IExceptionTelemetry>();
        using var webView = CreateWebView(_site.Options, timeProvider: time, telemetry: telemetry);
        await HoldDataFolderAsync();

        var ended = await StartUntilEndedAsync(webView);

        Assert.Equal(WebViewFailure.StartFailed, ended.Failure);

        // HRESULT_FROM_WIN32(ERROR_INVALID_STATE): the folder is still in use.
        Assert.Equal(unchecked((int)0x8007139F), Assert.IsType<COMException>(ended.Error).HResult);
        telemetry.Received(1).Track(ended.Error);
    }

    // The webview of the application, with the folders of the test in place of the ones of the installation.
    private WebViewHost CreateWebView(
        WebViewOptions options,
        WebViewProxy? proxy = null,
        TimeProvider? timeProvider = null,
        IExceptionTelemetry? telemetry = null,
        ILogger<WebViewHost>? logger = null)
    {
        var services = _host.Services;

        return new WebViewHost(
            options,
            services.GetRequiredService<IApiAddressProvider>(),
            services.GetRequiredService<ApiToken>(),
            proxy ?? services.GetRequiredService<WebViewProxy>(),
            services.GetRequiredService<WebViewFailurePolicy>(),
            timeProvider ?? TimeProvider.System,
            telemetry ?? services.GetRequiredService<IExceptionTelemetry>(),
            logger ?? services.GetRequiredService<ILogger<WebViewHost>>());
    }

    // The proxy server that the user named, for the endpoint of the telemetry that the page calls, with the
    // credentials that the user entered. It replaces the webview of the test, which has not started.
    private void UseProxy(TestProxy proxy, ICredentials credentials)
    {
        var settings = new ProxySettings { Type = ProxyType.Custom, Address = proxy.Address.Authority };
        _webView.Dispose();
        _webView = CreateWebView(_site.Options, new WebViewProxy(ProxySelection.For(settings), credentials, s_ingestion));
    }

    // What the proxy was asked on the way to the endpoint of the telemetry: the engine may also send requests of
    // its own through it.
    private static List<TestProxyRequest> IngestionRequests(TestProxy proxy)
    {
        return [.. proxy.Requests.Where(request => request.Target.StartsWith(s_ingestion.Host + ":", StringComparison.Ordinal))];
    }

    private IEnumerable<LogRecord> WebViewHostRecords()
    {
        return _dataFolder.Log.Buffer.Read(after: 0).Records.Where(record => record.Category == typeof(WebViewHost).FullName);
    }

    // Disposes the webview and waits until its engine says that the browser process has ended and has
    // released the data folder: the folder of the test can then be deleted.
    private static async Task<bool> CloseAsync(WebViewHost webView)
    {
        var environment = webView.Engine?.Environment;
        if (environment is null)
        {
            webView.Dispose();
            return false;
        }

        await CloseUntilExitedAsync(environment, webView.Dispose);
        return true;
    }

    // A browser process started with other arguments, as the one of an earlier run with another proxy: it holds
    // the data folder of the test until the test releases it, or until the end of the test.
    private async Task HoldDataFolderAsync()
    {
        _holder = await _window.InvokeAsync(async form =>
        {
            var options = new CoreWebView2EnvironmentOptions { AdditionalBrowserArguments = "--no-proxy-server" };
            var environment = await CoreWebView2Environment.CreateAsync(null, _site.Options.UserDataFolder, options);
            return await environment.CreateCoreWebView2ControllerAsync(form.Handle);
        });
    }

    private async Task ReleaseDataFolderAsync()
    {
        if (_holder is not { } holder)
        {
            return;
        }

        _holder = null;
        _ = await _window.InvokeAsync(async _ =>
        {
            await CloseUntilExitedAsync(holder.CoreWebView2.Environment, holder.Close);
            return true;
        });
    }

    // Closes what holds a browser process and waits until the process has ended and has released the data folder.
    private static async Task CloseUntilExitedAsync(CoreWebView2Environment environment, Action close)
    {
        var exited = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        environment.BrowserProcessExited += (_, _) => exited.TrySetResult();
        close();
        await exited.Task.WaitAsync(s_timeout);
    }

    // The page called the API and the API answered. When it did not, the message says what the host received of
    // the call and how it answered, and what the webview recorded on which runtime: whether the engine or the host
    // stopped the call.
    private async Task AssertTheApiAnsweredAsync(JsonElement report, IEnumerable<string>? webViewRecords = null)
    {
        var options = report.GetProperty("options").GetString();
        if (options == "status 200")
        {
            return;
        }

        var runtime = await _window.InvokeAsync(_ => Task.FromResult(_webView.Engine?.Environment.BrowserVersionString));
        var records = webViewRecords ?? WebViewHostRecords().Select(record => $"{record.Level} {record.EventId}: {record.Message}");
        Assert.Fail($"""
            The call of the page to the API: {options}.
            Requests that the host began: [{string.Join(", ", _requests.Requests)}]; answered: [{string.Join(", ", _requests.Answers)}].
            The runtime of the engine: {runtime ?? "none"}. The records of the webview: [{string.Join("; ", records)}].
            """);
    }

    // Starts the webview and waits for the end that a failure decided, then disposes it.
    private Task<WebViewEndedEventArgs> StartUntilEndedAsync(WebViewHost webView)
    {
        return _window.InvokeAsync(async form =>
        {
            var completion = new TaskCompletionSource<WebViewEndedEventArgs>();
            webView.Ended += (_, e) => completion.TrySetResult(e);
            webView.Start(form);
            var result = await completion.Task.WaitAsync(s_timeout);
            webView.Dispose();
            return result;
        });
    }

    private async Task<JsonElement> StartAndReadReportAsync()
    {
        var report = WaitForReportAsync((_, _) => true);
        await _window.InvokeAsync(form => _webView.Start(form));
        return await report;
    }

    // Runs a script in the page and waits for what the script writes, as JSON after a prefix, in the title
    // of the document when it has finished. What the script waits for can be done once it has run.
    private Task<JsonElement> RunInPageAsync(string prefix, string script, Action? afterScript = null)
    {
        return _window.InvokeAsync(async form =>
        {
            var engine = _webView.Engine!;
            var completion = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);

            void OnTitleChanged(object? sender, object e)
            {
                var title = engine.DocumentTitle;
                if (title.StartsWith(prefix, StringComparison.Ordinal))
                {
                    using var document = JsonDocument.Parse(title[prefix.Length..]);
                    completion.TrySetResult(document.RootElement.Clone());
                }
            }

            engine.DocumentTitleChanged += OnTitleChanged;
            try
            {
                _ = await engine.ExecuteScriptAsync(script);
                afterScript?.Invoke();
                return await completion.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
            }
            finally
            {
                engine.DocumentTitleChanged -= OnTitleChanged;
            }
        });
    }

    // Waits for a report of the page that the test is looking for. The page writes its report in the title
    // of the document, and the engine tells when the title changes: nothing is waited for on time.
    private Task<JsonElement> WaitForReportAsync(Func<CoreWebView2, JsonElement, bool> isExpected)
    {
        return _window.InvokeAsync(async _ =>
        {
            var completion = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
            CoreWebView2? watched = null;

            void Check()
            {
                var engine = _webView.Engine;
                if (engine is null)
                {
                    return;
                }

                if (!ReferenceEquals(engine, watched))
                {
                    watched = engine;
                    engine.DocumentTitleChanged += (_, _) => Check();
                }

                var title = engine.DocumentTitle;
                if (!title.StartsWith(WebViewTestSite.ReportPrefix, StringComparison.Ordinal))
                {
                    return;
                }

                using var document = JsonDocument.Parse(title[WebViewTestSite.ReportPrefix.Length..]);
                if (isExpected(engine, document.RootElement))
                {
                    completion.TrySetResult(document.RootElement.Clone());
                }
            }

            void OnEnded(object? sender, WebViewEndedEventArgs e)
            {
                completion.TrySetException(new InvalidOperationException($"The webview ended: {e.Failure}, {e.Error}"));
            }

            void OnPageLoaded(object? sender, EventArgs e)
            {
                Check();
            }

            _webView.PageLoaded += OnPageLoaded;
            _webView.Ended += OnEnded;
            try
            {
                Check();
                return await completion.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
            }
            finally
            {
                _webView.PageLoaded -= OnPageLoaded;
                _webView.Ended -= OnEnded;
            }
        });
    }
}
