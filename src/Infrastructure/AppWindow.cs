namespace Sqlbi.Bravo.Infrastructure
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;
    using Microsoft.Web.WebView2.Core;
    using Microsoft.Web.WebView2.WinForms;
    using Sqlbi.Bravo.Infrastructure.Configuration;
    using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Infrastructure.Messages;
    using Sqlbi.Bravo.Infrastructure.Windows.Interop;
    using Sqlbi.Bravo.Models;
    using System;
    using System.Drawing;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Windows.Forms;

    internal partial class AppWindow : Form
    {
        private readonly IHost _host;
        private readonly AppInstance _instance;
        private readonly Color _startupThemeColor;

        public AppWindow(IHost host, AppInstance instance)
        {
            _host = host;
            _instance = instance;
            _startupThemeColor = ThemeHelper.ShouldUseDarkMode(UserPreferences.Current.Theme) ? AppEnvironment.ThemeColorDark : AppEnvironment.ThemeColorLight;

            InitializeComponent();
            InitializeWebViewAsync();

            // How does the window manager decide where to place a newly-created window ? https://devblogs.microsoft.com/oldnewthing/20121126-00/?p=5993
            StartPosition = FormStartPosition.WindowsDefaultBounds;
        }

        private WebView2 WebView => webView;

        private async void InitializeWebViewAsync()
        {
            //await System.Threading.Tasks.Task.Delay(3_000);

            // @daniele
            // - removed index-dark.html
            // - added support for Settings.IsStatusBarEnabled
            // - added support for Settings.AreBrowserAcceleratorKeysEnabled
            // - should we remove file:// protocol and enable virtual hostname ?? (WebResourceRequested event not available, requires CONFIG rafactoring using web-message)

            WebView.Visible = false;

            var options = new CoreWebView2EnvironmentOptions(additionalBrowserArguments: null, language: null, targetCompatibleBrowserVersion: null, allowSingleSignOnUsingOSPrimaryAccount: false);
            {
                options.AdditionalBrowserArguments = "";
            }
            var environment = await CoreWebView2Environment.CreateAsync(browserExecutableFolder: null, userDataFolder: AppEnvironment.ApplicationTempPath, options);
            {
                //environment.BrowserProcessExited
            }
            await WebView.EnsureCoreWebView2Async(environment);
#if DEBUG
            var isDebug = true;
#else
            var isDebug = false;
#endif
            WebView.AllowExternalDrop = true;
            WebView.DefaultBackgroundColor = _startupThemeColor;
            WebView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = isDebug;
            WebView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = isDebug;
            WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = isDebug;
            WebView.CoreWebView2.Settings.AreDevToolsEnabled = isDebug;
            WebView.CoreWebView2.Settings.AreHostObjectsAllowed = false;
            WebView.CoreWebView2.Settings.IsWebMessageEnabled = true;
            WebView.CoreWebView2.Settings.IsScriptEnabled = true;
            WebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            //WebView.CoreWebView2.Settings.IsZoomControlEnabled = true;
            //WebView.CoreWebView2.Settings.IsBuiltInErrorPageEnabled = true;
            WebView.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;
            WebView.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
            WebView.CoreWebView2.Settings.IsPinchZoomEnabled = false;
            WebView.CoreWebView2.Settings.IsSwipeNavigationEnabled = false;
            //WebView.CoreWebView2.Settings.HiddenPdfToolbarItems = CoreWebView2PdfToolbarItems.None;
#if DEBUG
            WebView.CoreWebView2.OpenDevToolsWindow();
            //WebView.CoreWebView2.OpenTaskManagerWindow();
            WebView.CoreWebView2.PermissionRequested += OnWebViewPermissionRequested;
            WebView.CoreWebView2.NavigationStarting += OnWebViewNavigationStarting;
            WebView.CoreWebView2.NavigationCompleted += OnWebViewNavigationCompleted;
            WebView.CoreWebView2.ContentLoading += OnWebViewContentLoading;
            WebView.CoreWebView2.WebMessageReceived += OnWebViewWebWebMessageReceived;
            WebView.CoreWebView2.WebResourceResponseReceived += OnWebViewWebResourceResponseReceived;
#endif
            WebView.CoreWebView2.DOMContentLoaded += OnWebViewDOMContentLoaded;
            WebView.CoreWebView2.WebResourceRequested += OnWebViewWebResourceRequested;

            await WebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
window.external = {
    sendMessage: function(message) {
        window.chrome.webview.postMessage(message);
    },
    receiveMessage: function(callback) {
        window.chrome.webview.addEventListener('message', function(e) {
            callback(e.data);
        });
    }
};");
            WebView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
            WebView.Source = new Uri(Path.Combine(Environment.CurrentDirectory, "wwwroot\\index.html"));
            //WebView.CoreWebView2.SetVirtualHostNameToFolderMapping("bravo.example", "wwwroot", CoreWebView2HostResourceAccessKind.Allow);
            //WebView.CoreWebView2.Navigate("https://bravo.example/index.html"); // For '.example' see rfc6761
        }

        protected override void WndProc(ref Message message)
        {
            switch (message.Msg)
            {
                // Form.StyleChanged event detects all theme change except for Aero color changes
                // The Aero color change triggers the WM_DWMCOLORIZATIONCOLORCHANGED message
                case (int)WindowMessage.WM_DWMCOLORIZATIONCOLORCHANGED:
                case (int)WindowMessage.WM_DWMCOMPOSITIONCHANGED:
                case (int)WindowMessage.WM_THEMECHANGED:
                    if (UserPreferences.Current.Theme == ThemeType.Auto)
                    {
                        ThemeHelper.ChangeTheme(message.HWnd, ThemeType.Auto);
                    }
                    break;
            }

            base.WndProc(ref message);
        }

        private void OnFormLoad(object? sender, EventArgs e)
        {
            ThemeHelper.InitializeTheme(Handle, UserPreferences.Current.Theme);

            Text = AppEnvironment.ApplicationMainWindowTitle;
            BackgroundImageLayout = ImageLayout.Center;
            BackColor = _startupThemeColor;

            CenterToScreen();

            _instance.OnNewInstance += OnNewInstanceRestoreFormWindowToForeground;
        }

        private void OnFormClosed(object? sender, FormClosedEventArgs e)
        {
            _instance.OnNewInstance -= OnNewInstanceRestoreFormWindowToForeground;
            _instance.OnNewInstance -= OnNewInstanceSendStartupWebMessage;

            NotificationHelper.ClearNotifications();
        }

        private void OnWebViewDOMContentLoaded(object? sender, CoreWebView2DOMContentLoadedEventArgs e)
        {
            WebViewLog(message: $"::OnWebViewDOMContentLoaded({ e.NavigationId })");

            if (WebView.Visible == false)
            {
                WebView.Visible = true;
                SendAppStartupWebMessage();

                _instance.OnNewInstance += OnNewInstanceSendStartupWebMessage;
            }
        }

        private void OnWebViewPermissionRequested(object? sender, CoreWebView2PermissionRequestedEventArgs e)
        {
            WebViewLog(message: $"::OnWebViewPermissionRequested({ e.PermissionKind }|{ e.State })");
        }

        private void OnWebViewNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            WebViewLog(message: $"::OnWebViewNavigationStarting({ e.NavigationId }|{ e.Uri })");
        }

        private void OnWebViewNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            WebViewLog(message: $"::OnWebViewNavigationCompleted({ e.NavigationId }|{ e.IsSuccess }|{ e.WebErrorStatus })");
        }

        private void OnWebViewContentLoading(object? sender, CoreWebView2ContentLoadingEventArgs e)
        {
            WebViewLog(message: $"::OnWebViewContentLoading({ e.NavigationId }|{ e.IsErrorPage })");
        }

        private void OnWebViewWebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            WebViewLog(message: $"::OnWebViewWebResourceRequested({ e.ResourceContext }|{ e.Request.Uri })");

            if (e.ResourceContext == CoreWebView2WebResourceContext.Script && e.Request.Uri.EqualsI("app://config.js"))
            {
                var content = GetJSConfig();
                e.Response = WebView.CoreWebView2.Environment.CreateWebResourceResponse(content, StatusCodes.Status200OK, "OK", "Content-Type: text/javascript");
            }
        }

        private void OnWebViewWebWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            var messageString = e.TryGetWebMessageAsString();

            WebViewLog(message: $"::OnWebViewWebWebMessageReceived({ e.Source }|{ messageString })");
        }

        private void OnWebViewWebResourceResponseReceived(object? sender, CoreWebView2WebResourceResponseReceivedEventArgs e)
        {
            WebViewLog(message: $"::OnWebViewWebResourceResponseReceived({ e.Response.StatusCode }{ e.Response.ReasonPhrase }|{ e.Request.Uri })");
        }

        private void OnNewInstanceRestoreFormWindowToForeground(object? sender, AppInstanceStartupEventArgs _)
        {
            Invoke(() =>
            {
                if (WindowState == FormWindowState.Minimized)
                {
                    User32.ShowWindow(Handle, User32.SW_RESTORE);
                }

                User32.SetForegroundWindow(Handle);
            });
        }

        private void OnNewInstanceSendStartupWebMessage(object? sender, AppInstanceStartupEventArgs e)
        {
            if (e.Message?.IsEmpty == false)
            {
                Invoke(() =>
                {
                    var webMessageString = e.Message.ToWebMessageString();
                    WebView.CoreWebView2.PostWebMessageAsString(webMessageString);
                });
            }
        }

        private Stream GetJSConfig()
        {
            var config = new
            {
#if DEBUG
                debug = true,
#endif
                address = GetAddress(),
                token = AppEnvironment.ApiAuthenticationToken,
                version = AppEnvironment.ApplicationProductVersion,
                build = AppEnvironment.ApplicationFileVersion,
                options = BravoOptions.CreateFromUserPreferences(),
                culture = new
                {
                    ietfLanguageTag = CultureInfo.CurrentCulture.IetfLanguageTag,
                    twoLetterISOLanguageName = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                },
                telemetry = new
                {
                    instrumentationKey = AppEnvironment.TelemetryInstrumentationKey,
                    contextDeviceOperatingSystem = AppTelemetryInitializer.DeviceOperatingSystem,
                    contextComponentVersion = AppTelemetryInitializer.ComponentVersion,
                    contextSessionId = AppTelemetryInitializer.SessionId,
                    contextUserId = AppTelemetryInitializer.UserId,
                    globalProperties = AppTelemetryInitializer.GlobalProperties
                },
            };

            var script = $@"var CONFIG = { JsonSerializer.Serialize(config, AppEnvironment.DefaultJsonOptions) };";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(script));

            return stream;

            string GetAddress()
            {
                var address = _host.GetListeningAddresses().Single(); // single address expected here
                var addressString = address.ToString();

                return addressString;
            }
        }

        private void SendAppStartupWebMessage()
        {
            var startupSettingsOptions = _host.Services.GetService(typeof(IOptions<StartupSettings>)) as IOptions<StartupSettings>;
            if (startupSettingsOptions is not null)
            {
                var startupSettings = startupSettingsOptions.Value;
                if (startupSettings.IsEmpty == false)
                {
                    var startupMessage = AppInstanceStartupMessage.CreateFrom(startupSettingsOptions.Value);
                    var startupMessageString = startupMessage.ToWebMessageString();

                    WebView.CoreWebView2.PostWebMessageAsString(startupMessageString);
                }
            }
        }

        private void WebViewLog(string message)
        {
#if DEBUG
            WebView.CoreWebView2.ExecuteScriptAsync($"console.log('{ message }');");
#endif
            //if (AppEnvironment.IsDiagnosticLevelVerbose)
            //    AppEnvironment.AddDiagnostics(DiagnosticMessageType.Text, name: $"{ nameof(AppWindow) }.{ nameof(WebView2) }", content: message);
        }
    }
}
