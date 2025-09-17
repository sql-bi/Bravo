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
    using Sqlbi.Bravo.Infrastructure.Security;
    using Sqlbi.Bravo.Infrastructure.Services;
    using Sqlbi.Bravo.Infrastructure.Windows.Interop;
    using Sqlbi.Bravo.Models;
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Windows.Forms;

    internal partial class AppWindow : Form
    {
        private const string WindowExternalWebMessageCallbackScript = @"
window.external = {
    sendMessage: function(message) {
        window.chrome.webview.postMessage(message);
    },
    receiveMessage: function(callback) {
        window.chrome.webview.addEventListener('message', function(e) {
            callback(e.data);
        });
    }
};";
        public static SynchronizationContext? UISynchronizationContext { get; set; }

        private readonly IHost _host;
        private readonly AppInstance _instance;
        private readonly Color _startupThemeColor;

        public AppWindow(IHost host, AppInstance instance)
        {
            _host = host;
            _instance = instance;
            _startupThemeColor = ThemeHelper.ShouldUseDarkMode(UserPreferences.Current.Theme) ? AppEnvironment.ThemeColorDark : AppEnvironment.ThemeColorLight;

            UISynchronizationContext = new WindowsFormsSynchronizationContext();
            InitializeComponent();
            InitializeWebViewAsync();

            // How does the window manager decide where to place a newly-created window ? https://devblogs.microsoft.com/oldnewthing/20121126-00/?p=5993
            StartPosition = FormStartPosition.WindowsDefaultBounds;
        }

        private WebView2 WebView => webView;

        private async void InitializeWebViewAsync()
        {
            //
            // Feature-detecting to test whether the installed Runtime supports recently added APIs
            // https://docs.microsoft.com/en-us/microsoft-edge/webview2/concepts/versioning#feature-detecting-to-test-whether-the-installed-runtime-supports-recently-added-apis
            //
            // ICoreWebView2_2          - SDK >= 1.0.705.50  - Runtime >= 86.0.616.0
            // ICoreWebView2_10         - SDK >= 1.0.1150.38 - Runtime >= 99.0.1150.38
            // ICoreWebView2Settings3   - SDK >= 1.0.864.35  - Runtime >= 91.0.864.35
            // ICoreWebView2Settings4   - SDK >= 1.0.902.49  - Runtime >= 92.0.902.49
            // ICoreWebView2Settings5   - SDK >= 1.0.902.49  - Runtime >= 92.0.902.49
            // ICoreWebView2Settings6   - SDK >= 1.0.992.28  - Runtime >= 94.0.992.31
            // ICoreWebView2Controller2 - SDK >= 1.0.774.44  - Runtime >= 89.0.774.44
            //
            // How to test a specific runtime version:
            //
            // - winget (not all versions are available)
            //   winget show --id=Microsoft.EdgeWebView2Runtime --versions
            //   winget install --id=Microsoft.EdgeWebView2Runtime --version 95.0.1020.53 --architecture x64
            // - Download installer from https://www.catalog.update.microsoft.com/Search.aspx?q=WebView2
            //   uncompress microsoftedgestandaloneinstallerx64_<guid>.exe
            //   uncompress MicrosoftEdge_X64_<version>.exe.{<guid>}
            //   create webview environment and pass the folder path in then 'browserExecutableFolder' arguments => .\microsoftedgestandaloneinstallerx64_<guid>\MicrosoftEdge_X64_<version>\MSEDGE\Chrome-bin\<version>

            WebView.Visible = false;

            var options = new CoreWebView2EnvironmentOptions(additionalBrowserArguments: null, language: null, targetCompatibleBrowserVersion: null, allowSingleSignOnUsingOSPrimaryAccount: false);
            {
                /* ICoreWebView2EnvironmentOptions */ options.AdditionalBrowserArguments = WebView2Helper.GetProxyArguments(UserPreferences.Current.Proxy, WebProxyWrapper.Current.DefaultSystemProxy);
                
                if (AppEnvironment.IsDiagnosticLevelVerbose)
                    AppEnvironment.AddDiagnostics(DiagnosticMessageType.Text, name: $"{ nameof(AppWindow) }.{ nameof(InitializeWebViewAsync) }.{ nameof(options.AdditionalBrowserArguments) }", content: options.AdditionalBrowserArguments);
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
            /* ICoreWebView2Controller2 */ WebView2Helper.TryAndIgnoreUnsupportedError(() => WebView.DefaultBackgroundColor = _startupThemeColor);
            /* ICoreWebView2Controller4 */ // WebView2Helper.TryAndIgnoreUnsupportedError(() => WebView.AllowExternalDrop = true); // Commented out because the default value is true
            /* ICoreWebView2Settings3   */ WebView2Helper.TryAndIgnoreUnsupportedError(() => WebView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = isDebug);
            /* ICoreWebView2Settings    */ WebView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = isDebug;
            /* ICoreWebView2Settings    */ WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = isDebug;
            /* ICoreWebView2Settings    */ WebView.CoreWebView2.Settings.AreDevToolsEnabled = isDebug;
            /* ICoreWebView2Settings    */ WebView.CoreWebView2.Settings.AreHostObjectsAllowed = false;
            /* ICoreWebView2Settings    */ WebView.CoreWebView2.Settings.IsWebMessageEnabled = true;
            /* ICoreWebView2Settings    */ WebView.CoreWebView2.Settings.IsScriptEnabled = true;
            /* ICoreWebView2Settings    */ WebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            /* ICoreWebView2Settings4   */ WebView2Helper.TryAndIgnoreUnsupportedError(() => WebView.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false);
            /* ICoreWebView2Settings4   */ WebView2Helper.TryAndIgnoreUnsupportedError(() => WebView.CoreWebView2.Settings.IsGeneralAutofillEnabled = false);
            /* ICoreWebView2Settings5   */ WebView2Helper.TryAndIgnoreUnsupportedError(() => WebView.CoreWebView2.Settings.IsPinchZoomEnabled = false);
            /* ICoreWebView2Settings6   */ WebView2Helper.TryAndIgnoreUnsupportedError(() => WebView.CoreWebView2.Settings.IsSwipeNavigationEnabled = false);
#if DEBUG_WWWROOT
            WebView.CoreWebView2.OpenDevToolsWindow();
            //WebView.CoreWebView2.OpenTaskManagerWindow();
            //WebView.CoreWebView2.NavigationStarting += OnWebViewNavigationStarting;
            //WebView.CoreWebView2.NavigationCompleted += OnWebViewNavigationCompleted;
            //WebView.CoreWebView2.ContentLoading += OnWebViewContentLoading;
            //WebView.CoreWebView2.WebMessageReceived += OnWebViewWebWebMessageReceived;
            //WebView.CoreWebView2.WebResourceResponseReceived += OnWebViewWebResourceResponseReceived;
#endif
            /* ICoreWebView2_2  */ WebView.CoreWebView2.DOMContentLoaded += OnWebViewDOMContentLoaded;
            /* ICoreWebView2    */ WebView.CoreWebView2.WebResourceRequested += OnWebViewWebResourceRequested;
            /* ICoreWebView2    */ WebView.CoreWebView2.PermissionRequested += OnWebViewPermissionRequested;
            /* ICoreWebView2_10 */ WebView2Helper.TryAndIgnoreUnsupportedError(() => WebView.CoreWebView2.BasicAuthenticationRequested += OnWebViewBasicAuthenticationRequested);

            /* ICoreWebView2 */ await WebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(WindowExternalWebMessageCallbackScript);
            /* ICoreWebView2 */ WebView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
            WebView.Source = new Uri(Path.Combine(Environment.CurrentDirectory, "wwwroot\\index.html"));
            // /* ICoreWebView2_3 */ WebView.CoreWebView2.SetVirtualHostNameToFolderMapping("bravo.example", "wwwroot", CoreWebView2HostResourceAccessKind.Allow);
            //WebView.CoreWebView2.Navigate("https://bravo.example/index.html"); // For '.example' see rfc6761

            // Allow users to open the DevTools for troubleshooting; this is only available in non-stable releases
            if (!AppEnvironment.IsStableRelease && CommonHelper.IsKeyDown(System.Windows.Forms.Keys.ShiftKey))
                WebView.CoreWebView2.OpenDevToolsWindow();
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

            var titleVersionHidden = Microsoft.Win32.Registry.CurrentUser.GetBoolValue(subkeyName: AppEnvironment.ApplicationRegistryKeyName, valueName: AppEnvironment.ApplicationRegistryApplicationTitleVersionHiddenValue);

            Text = titleVersionHidden ? AppEnvironment.ApplicationMainWindowTitle : AppEnvironment.ApplicationMainWindowTitle.AppendApplicationVersion();
            BackgroundImageLayout = ImageLayout.Center;
            BackColor = _startupThemeColor;

            CenterToScreen();

            _instance.OnNewInstance += OnNewInstanceRestoreFormWindowToForeground;
        }

        private void OnFormClosed(object? sender, FormClosedEventArgs e)
        {
            _instance.OnNewInstance -= OnNewInstanceRestoreFormWindowToForeground;
            _instance.OnNewInstance -= OnNewInstanceSendStartupWebMessage;
        }

        private void OnWebViewDOMContentLoaded(object? sender, CoreWebView2DOMContentLoadedEventArgs e)
        {
            WebViewLog(message: $"::OnWebViewDOMContentLoaded({ e.NavigationId })");

            if (WebView.Visible == false)
            {
                WebView.Visible = true;
                BackgroundImage = null;
                SendAppStartupWebMessage();

                _instance.OnNewInstance += OnNewInstanceSendStartupWebMessage;
            }
        }

        private void OnWebViewPermissionRequested(object? sender, CoreWebView2PermissionRequestedEventArgs e)
        {
            WebViewLog(message: $"::OnWebViewPermissionRequested({ e.PermissionKind }|{ e.State })");

            if (e.PermissionKind == CoreWebView2PermissionKind.ClipboardRead)
                e.State = CoreWebView2PermissionState.Allow;
        }

        private void OnWebViewBasicAuthenticationRequested(object? sender, CoreWebView2BasicAuthenticationRequestedEventArgs e)
        {
            var proxy = UserPreferences.Current.Proxy;
            if (proxy?.Type != ProxyType.None && proxy?.UseDefaultCredentials == false)
            {
                if (TelemetryHelper.IsTelemetryUri(e.Uri))
                {
                    if (CredentialManager.TryGetCredential(targetName: AppEnvironment.CredentialManagerProxyCredentialName, out var genericCredential))
                    {
                        var credential = genericCredential.ToNetworkCredential();
                        if (credential is not null)
                        {
                            e.Response.UserName = credential.UserName;
                            e.Response.Password = credential.Password;
                            return;
                        }
                    }
                }
            }

            //var deferral = e.GetDeferral();

            //SynchronizationContext.Current?.Post((_) =>
            //{
            //    using (deferral)
            //    {
            //        var credentialOptions = new CredentialDialogOptions(caption: "Authentication request", message: $"Authentication request from { e.Uri }\r\nChallenge: { e.Challenge }")
            //        {
            //            HwndParent = Handle
            //        };

            //        var credential = CredentialDialog.PromptForCredentials(credentialOptions);
            //        if (credential is not null)
            //        {
            //            e.Response.UserName = credential.UserName;
            //            e.Response.Password = credential.Password;
            //        }
            //        else
            //        {
            //            e.Cancel = true;
            //        }
            //    }
            //}, state: null);
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
            //WebViewLog(message: $"::OnWebViewWebResourceRequested({ e.ResourceContext }|{ e.Request.Uri })");

            if (e.ResourceContext == CoreWebView2WebResourceContext.Script && e.Request.Uri.EqualsI("app://config.js"))
            {
                var content = GetConfigJs();
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
            ProcessHelper.InvokeOnUIThread(this, () =>
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
                ProcessHelper.InvokeOnUIThread(this, () =>
                {
                    var webMessageString = e.Message.ToWebMessageString();
                    WebView.CoreWebView2.PostWebMessageAsString(webMessageString);
                });
            }
        }

        private Stream GetConfigJs()
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
                policies = BravoPolicies.Current,
                culture = new
                {
                    ietfLanguageTag = CultureInfo.CurrentCulture.IetfLanguageTag,
                    twoLetterISOLanguageName = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                },
                telemetry = new
                {
                    instrumentationKey = AppEnvironment.TelemetryInstrumentationKey,
                    connectionString = AppEnvironment.TelemetryConnectionString,
                    contextDeviceOperatingSystem = AppTelemetryInitializer.DeviceOperatingSystem,
                    contextComponentVersion = AppTelemetryInitializer.ComponentVersion,
                    contextSessionId = AppTelemetryInitializer.SessionId,
                    contextUserId = AppTelemetryInitializer.UserId,
                    globalProperties = AppTelemetryInitializer.GlobalProperties
                },
            };

            // TODO: use AppEnvironment.DefaultJsonOptions instead - see AddJsonOptions in HostingExtensions.AddAndConfigureControllers
            var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web) { MaxDepth = 32 };
            jsonOptions.Converters.Add(new JsonStringEnumMemberConverter()); // https://github.com/dotnet/runtime/issues/31081#issuecomment-578459083

            var script = $@"var CONFIG = { JsonSerializer.Serialize(config, jsonOptions) };";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(script));

            return stream;

            string GetAddress()
            {
                var address = _host.GetListeningAddress(); 
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

        [Conditional("DEBUG")]
        private void WebViewLog(string message)
        {
            WebView.CoreWebView2.ExecuteScriptAsync($"console.log('[WEBVIEW]{ message }');");

            //if (AppEnvironment.IsDiagnosticLevelVerbose)
            //    AppEnvironment.AddDiagnostics(DiagnosticMessageType.Text, name: $"{ nameof(AppWindow) }.{ nameof(WebView2) }", content: message);
        }
    }
}
