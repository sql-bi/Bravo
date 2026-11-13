using System;
using System.Diagnostics;
using System.Drawing;
using System.Text.Json;
using System.Threading.Tasks;
using Bravo.Api.Security;
using Bravo.Api.Server;
using Bravo.Core.Settings;
using Bravo.Desktop.Development;
using Bravo.Desktop.Diagnostics;
using Bravo.Desktop.Hosting;
using Bravo.Desktop.Tests.Hosting;
using Bravo.Desktop.Tests.SingleInstance;
using Bravo.Desktop.WebView;
using Bravo.Platform.Windows.SingleInstance;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
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

    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(60);

    private readonly OwnedInstance _instance = new();
    private readonly WebViewTestSite _site = new();
    private readonly WebViewTestWindow _window = new();
    private readonly WebApplication _host;
    private readonly RequestLog _requests;
    private readonly WebViewHost _webView;

    public WebViewHostTests()
    {
        var identity = BuildIdentity.Development("1.1.0");
        var fatalExceptionHandler = Substitute.For<IFatalExceptionHandler>();
        _host = DesktopHost.Build(identity, DevelopmentOptions.None, _instance.Ownership, new SingleInstanceOptions(), fatalExceptionHandler);
        _requests = new RequestLog(_host.Services.GetRequiredService<DiagnosticListener>());

        // The webview of the application, with the folders of the test in place of the ones of the installation.
        _webView = new WebViewHost(
            _site.Options,
            _host.Services.GetRequiredService<IApiAddressProvider>(),
            _host.Services.GetRequiredService<ApiToken>(),
            _host.Services.GetRequiredService<WebViewFailurePolicy>());
    }

    public async ValueTask InitializeAsync()
    {
        await _host.StartAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _window.InvokeAsync(_ => CloseAsync(_webView));
        await _window.DisposeAsync();
        _requests.Dispose();
        await _host.DisposeAsync();
        _instance.Dispose();
        _site.Dispose();
    }

    [Fact]
    public async Task Start_Page_IsServedFromTheContentFolderUnderTheOriginOfThePage()
    {
        await StartAndReadReportAsync();

        var source = await _window.InvokeAsync(_ => Task.FromResult(_webView.Engine!.Source));
        Assert.Equal("https://bravo.invalid/index.html", source);
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

        Assert.Equal("status 200", report.GetProperty("options").GetString());
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
        Assert.Equal("status 200", report.GetProperty("options").GetString());
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
        Assert.Equal("status 200", report.GetProperty("options").GetString());
    }

    [Fact]
    public async Task Start_ContentFolderWithoutThePage_EndsWithTheCodeOfAMissingUserInterface()
    {
        using var site = new WebViewTestSite(withPage: false);
        var policy = _host.Services.GetRequiredService<WebViewFailurePolicy>();
        var address = _host.Services.GetRequiredService<IApiAddressProvider>();
        using var webView = new WebViewHost(site.Options, address, _host.Services.GetRequiredService<ApiToken>(), policy);

        var ended = await _window.InvokeAsync(async form =>
        {
            var completion = new TaskCompletionSource<WebViewEndedEventArgs>();
            webView.Ended += (_, e) => completion.TrySetResult(e);
            webView.Start(form);
            var result = await completion.Task.WaitAsync(s_timeout);
            webView.Dispose();
            return result;
        });

        Assert.Equal(WebViewFailure.ContentMissing, ended.Failure);
        Assert.Equal(ExitCode.UserInterfaceMissing, ended.ExitCode);
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

        var released = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        environment.BrowserProcessExited += (_, _) => released.TrySetResult();
        webView.Dispose();
        await released.Task.WaitAsync(s_timeout);
        return true;
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
