using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Bravo.Api.Security;
using Bravo.Api.Server;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Bravo.Desktop.WebView;

/// <summary>
/// The webview of the main window, and the one place that knows its engine, WebView2. It serves the page
/// from the content folder under the origin of <see cref="PageAddress"/>, gives the page the connection to
/// the API, keeps the page inside its origin, and answers the failures of the engine as
/// <see cref="WebViewFailurePolicy"/> decides. Everything runs on the thread of the control that hosts it.
/// </summary>
internal sealed class WebViewHost(
    WebViewOptions options,
    IApiAddressProvider apiAddress,
    ApiToken apiToken,
    WebViewFailurePolicy policy) : IDisposable
{
    private readonly WebViewOptions _options = options;
    private readonly IApiAddressProvider _apiAddress = apiAddress;
    private readonly ApiToken _apiToken = apiToken;
    private readonly WebViewFailurePolicy _policy = policy;

    private Control? _parent;
    private Color _backgroundColor;
    private CoreWebView2Environment? _environment;
    private WebView2? _view;
    private bool _hasEnded;
    private bool _isDisposed;

    /// <summary>
    /// Raised each time a document of the page has loaded: at the start, and after a recovery.
    /// </summary>
    public event EventHandler? PageLoaded;

    /// <summary>
    /// Raised once, when a failure ends the application. The handler tells the user and closes the window.
    /// </summary>
    public event EventHandler<WebViewEndedEventArgs>? Ended;

    /// <summary>
    /// The engine of the current control, once it was created.
    /// </summary>
    internal CoreWebView2? Engine => _view?.CoreWebView2;

    /// <summary>
    /// Creates the control inside <paramref name="parent"/> and opens the page. The control stays hidden
    /// until the first document has loaded: until then the parent shows through.
    /// </summary>
    /// <param name="parent">The control that the webview fills. Its handle exists.</param>
    /// <param name="backgroundColor">What the webview shows where the page draws nothing.</param>
    public void Start(Control parent, Color backgroundColor)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        _parent = parent;
        _backgroundColor = backgroundColor;
        Observe(CreateViewAsync());
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        DisposeView();
    }

    // Nothing awaits a step of the view. A failure of the engine becomes a decision here; any other
    // exception goes on to the message loop, which it leaves: the application ends as for every error
    // that nothing expected.
    private async void Observe(Task step)
    {
        try
        {
            await step.ConfigureAwait(true);
        }
        catch (WebView2RuntimeNotFoundException exception)
        {
            Handle(WebViewFailure.RuntimeMissing, exception);
        }
        catch (Exception exception) when (IsEngineFailure(exception))
        {
            Handle(WebViewFailure.StartFailed, exception);
        }
    }

    // What the engine reports its failures with: errors of COM, of the data folder, of a runtime that lacks
    // an interface, of a control that the end of the browser process left unusable.
    private static bool IsEngineFailure(Exception exception)
    {
        return exception is COMException
            or UnauthorizedAccessException
            or IOException
            or InvalidOperationException
            or ArgumentException
            or NotImplementedException
            or InvalidCastException;
    }

    private async Task CreateViewAsync()
    {
        if (!File.Exists(Path.Combine(_options.ContentFolder, PageAddress.IndexFileName)))
        {
            Handle(WebViewFailure.ContentMissing, error: null);
            return;
        }

        var environment = await CoreWebView2Environment
            .CreateAsync(browserExecutableFolder: null, _options.UserDataFolder, options: null)
            .ConfigureAwait(true);
        if (_isDisposed)
        {
            return;
        }

        // Raised when the browser process has ended and has released the data folder. An environment that a
        // new one replaced is no longer listened to.
        _environment = environment;
        environment.BrowserProcessExited += (_, e) => OnBrowserProcessExited(environment, e);

        var view = new WebView2 { Dock = DockStyle.Fill, Visible = false, DefaultBackgroundColor = _backgroundColor };
        _view = view;
        _parent!.Controls.Add(view);
        await view.EnsureCoreWebView2Async(environment).ConfigureAwait(true);
        if (_isDisposed)
        {
            return;
        }

        var engine = view.CoreWebView2;
        Restrict(engine.Settings);
        engine.NavigationStarting += OnNavigationStarting;
        engine.FrameNavigationStarting += OnFrameNavigationStarting;
        engine.NewWindowRequested += OnNewWindowRequested;
        engine.PermissionRequested += OnPermissionRequested;
        engine.DownloadStarting += OnDownloadStarting;
        engine.NavigationCompleted += OnNavigationCompleted;
        engine.ProcessFailed += OnProcessFailed;

        // Awaited before the navigation: the script is then sure to run in the first document.
        var script = PageConnectionScript.Create(_apiAddress.GetAddress(), _apiToken.Value);
        _ = await engine.AddScriptToExecuteOnDocumentCreatedAsync(script).ConfigureAwait(true);
        if (_isDisposed)
        {
            return;
        }

        // No other origin needs the files: none may read them.
        engine.SetVirtualHostNameToFolderMapping(
            PageAddress.HostName,
            _options.ContentFolder,
            CoreWebView2HostResourceAccessKind.Deny);
        engine.Navigate(PageAddress.Start);
    }

    // Everything that the page does not use is off.
    private void Restrict(CoreWebView2Settings settings)
    {
        settings.AreHostObjectsAllowed = false;
        settings.IsWebMessageEnabled = false;
        settings.AreDefaultScriptDialogsEnabled = false;
        settings.AreDefaultContextMenusEnabled = false;
        settings.IsStatusBarEnabled = false;
        settings.IsPasswordAutosaveEnabled = false;
        settings.IsGeneralAutofillEnabled = false;
        settings.IsPinchZoomEnabled = false;
        settings.IsSwipeNavigationEnabled = false;

        // The page never leaves its origin and no download starts: there is no address to ask a reputation for.
        settings.IsReputationCheckingRequired = false;

        settings.AreDevToolsEnabled = _options.AreDeveloperToolsEnabled;
        settings.AreBrowserAcceleratorKeysEnabled = _options.AreDeveloperToolsEnabled;
    }

    private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        e.Cancel = !PageAddress.Contains(e.Uri);
    }

    // A frame may also hold a blank document, which is of the origin of the page that created it.
    private void OnFrameNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        var isBlank = e.Uri.StartsWith("about:", StringComparison.OrdinalIgnoreCase);
        e.Cancel = !isBlank && !PageAddress.Contains(e.Uri);
    }

    // Handled, with no window given: none opens.
    private void OnNewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        e.Handled = true;
    }

    // An answer of the application is given at every request and is not kept in the profile, so a later
    // version decides again. Where the application does not decide, the request is left as it arrived:
    // the engine asks the user.
    private void OnPermissionRequested(object? sender, CoreWebView2PermissionRequestedEventArgs e)
    {
        if (WebViewPermissionRule.Decide(e.PermissionKind, e.Uri) is { } state)
        {
            e.State = state;
            e.SavesInProfile = false;
        }
    }

    private void OnDownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
    {
        e.Cancel = true;
        e.Handled = true;
    }

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (_view is { Visible: false } view)
        {
            view.Visible = true;
        }

        PageLoaded?.Invoke(this, EventArgs.Empty);
    }

    private void OnProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
    {
        // The end of the browser process is answered when the environment says that the process has gone.
        var failure = e.ProcessFailedKind == CoreWebView2ProcessFailedKind.RenderProcessExited
            ? WebViewFailure.RenderProcessExited
            : WebViewFailure.Transient;
        Handle(failure, error: null);
    }

    private void OnBrowserProcessExited(CoreWebView2Environment environment, CoreWebView2BrowserProcessExitedEventArgs e)
    {
        // A normal exit is the one of a control that was closed.
        var hasFailed = e.BrowserProcessExitKind == CoreWebView2BrowserProcessExitKind.Failed;
        if (hasFailed && ReferenceEquals(environment, _environment))
        {
            Handle(WebViewFailure.BrowserProcessExited, error: null);
        }
    }

    private void Handle(WebViewFailure failure, Exception? error)
    {
        if (_isDisposed || _hasEnded)
        {
            return;
        }

        var decision = _policy.Decide(failure);
        switch (decision.Recovery)
        {
            case WebViewRecovery.None:
                break;
            case WebViewRecovery.Reload:
                Post(ReloadPage);
                break;
            case WebViewRecovery.Recreate:
                Post(RecreateView);
                break;
            case WebViewRecovery.End:
                _hasEnded = true;
                var ended = new WebViewEndedEventArgs(failure, decision.ExitCode, error);
                Post(() => Ended?.Invoke(this, ended));
                break;
            default:
                throw new InvalidOperationException($"No action for the recovery {decision.Recovery}.");
        }
    }

    // After the handler of the engine has returned: the engine must not be called, and no dialog shown,
    // from inside one of its events.
    private void Post(Action action)
    {
        if (_parent is { IsHandleCreated: true, IsDisposed: false } parent)
        {
            _ = parent.BeginInvoke(() =>
            {
                if (!_isDisposed)
                {
                    action();
                }
            });
        }
    }

    // A recovery that was decided before a failure ended the application is not carried out.
    private void ReloadPage()
    {
        if (!_hasEnded)
        {
            Observe(ReloadPageAsync());
        }
    }

    private Task ReloadPageAsync()
    {
        _view?.CoreWebView2?.Reload();
        return Task.CompletedTask;
    }

    private void RecreateView()
    {
        if (!_hasEnded)
        {
            DisposeView();
            Observe(CreateViewAsync());
        }
    }

    // Disposing the control closes its controller, which ends the processes of the engine.
    private void DisposeView()
    {
        if (_view is not { } view)
        {
            return;
        }

        _view = null;
        _environment = null;
        view.Parent?.Controls.Remove(view);
        view.Dispose();
    }
}
