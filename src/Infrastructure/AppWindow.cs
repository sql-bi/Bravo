using AutoUpdaterDotNET;
using Bravo.Infrastructure.Windows.Interop;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PhotinoNET;
using Sqlbi.Bravo.Infrastructure.Configuration.Options;
using Sqlbi.Bravo.Infrastructure.Extensions;
using Sqlbi.Bravo.Infrastructure.Helpers;
using Sqlbi.Bravo.Infrastructure.Messages;
using Sqlbi.Bravo.Infrastructure.Windows.Interop;
using Sqlbi.Bravo.Models;
using Sqlbi.Infrastructure.Configuration.Settings;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Sqlbi.Bravo.Infrastructure
{
    internal class AppWindow : IDisposable
    {
        private readonly IHost _host;
        private readonly PhotinoWindow _window;
        private readonly UserSettings _userSettings;
        private readonly StartupSettings _startupSettings;

        private AppWindowSubclass? _windowSubclass;
        private bool _disposed;

        public AppWindow(IHost host)
        {
            _host = host;
            _window = CreateWindow();
            _userSettings = (_host.Services.GetService(typeof(IWritableOptions<UserSettings>)) as IWritableOptions<UserSettings> ?? throw new BravoUnexpectedException("UserSettings is null")).Value;
            _startupSettings = (_host.Services.GetService(typeof(IOptions<StartupSettings>)) as IOptions<StartupSettings> ?? throw new BravoUnexpectedException("StartupSettings is null")).Value;
        }

        private PhotinoWindow CreateWindow()
        {
#if DEBUG
            var contextMenuEnabled = true;
            var devToolsEnabled = true;
            var logVerbosity = 3;
#else
            var contextMenuEnabled = false;
            var devToolsEnabled = false;
            var logVerbosity = 0;
#endif
            var window = new PhotinoWindow()
                .SetIconFile("wwwroot/bravo.ico")
                .SetTitle(AppConstants.ApplicationMainWindowTitle)
                .SetTemporaryFilesPath(AppConstants.ApplicationFolderTempDataPath)
                .SetContextMenuEnabled(contextMenuEnabled)
                .SetDevToolsEnabled(devToolsEnabled)
                .SetLogVerbosity(logVerbosity) // 0 = Critical Only, 1 = Critical and Warning, 2 = Verbose, >2 = All Details. Default is 2.
                .SetGrantBrowserPermissions(true)
                .SetUseOsDefaultSize(true)
                .RegisterCustomSchemeHandler("app", CustomSchemeHandler)
                .Load("wwwroot/index.html")
                .Center();

            window.WindowCreating += OnWindowCreating;
            window.WindowCreated += OnWindowCreated;
            window.WindowClosing += OnWindowClosing;

            return window;
        }

        private Stream CustomSchemeHandler(object sender, string scheme, string url, out string contentType)
        {
            contentType = "text/javascript";

            var config = new
            {
                address = GetAddress().ToString(),
                version = AppConstants.ApplicationFileVersion,
                options = BravoOptions.CreateFrom(_userSettings),
                telemetry = new
                {
                    instrumentationKey = AppConstants.TelemetryInstrumentationKey,
                    contextDeviceOperatingSystem = ContextTelemetryInitializer.DeviceOperatingSystem,
                    contextComponentVersion = ContextTelemetryInitializer.ComponentVersion,
                    contextSessionId = ContextTelemetryInitializer.SessionId,
                    contextUserId = ContextTelemetryInitializer.UserId,
                },
            };
            config.options.Theme = GetTheme();

            var script = $@"var CONFIG = { JsonSerializer.Serialize(config, AppConstants.DefaultJsonOptions) };";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(script));

            return stream;

            Uri GetAddress()
            {
                var address = _host.GetListeningAddresses().Single(); // single address expected here
                return address;
            }

            ThemeType GetTheme()
            {
                var theme = _userSettings.Theme;

                if (theme == ThemeType.Auto)
                    theme = Uxtheme.IsSystemUsingDarkMode() ? ThemeType.Dark : ThemeType.Light;

                return theme;
            }
        }

        private void OnWindowCreating(object? sender, EventArgs e)
        {
            Trace.WriteLine($"::Bravo:INF:OnWindowCreating:{ _window.Title }");

            if (_userSettings.Theme != ThemeType.Auto) 
            {
                // Set the startup theme based on the latest settings saved by the user
                Uxtheme.SetStartupTheme(useDark: _userSettings.Theme == ThemeType.Dark);
            }
        }

        private void OnWindowCreated(object? sender, EventArgs e)
        {
            Trace.WriteLine($"::Bravo:INF:OnWindowCreated:{ _window.Title } ( { string.Join(", ", _host.GetListeningAddresses().Select((a) => a.ToString())) } )");
#if !DEBUG
            HandleHotKeys(register: true);
#endif   
            _windowSubclass = AppWindowSubclass.HookWindow(_window, _host);
         
            if (_startupSettings.IsExternalTool)
            {
                //_window.SendWebMessage(message);
            }

            CheckForUpdate();
        }

        private bool OnWindowClosing(object sender, EventArgs e)
        {
#if !DEBUG
            HandleHotKeys(register: false);
#endif
            return false; // Returning true stops window from closing
        }

        /// <summary>
        /// Register hotkeys in order to override WebView2 accelerator keys like Ctrl-P for print, Ctrl-R and F5 for reload ecc..
        /// </summary>
        private void HandleHotKeys(bool register)
        {
            // TODO: instead of register hot-keys we should use property 'Microsoft.Web.WebView2.Core.CoreWebView2Settings.AreBrowserAcceleratorKeysEnabled'
            // Unfortunatly CoreWebView2Settings.AreBrowserAcceleratorKeysEnabled property is not yet implemented on Photino.NET

            const int HOTKEY_CONTROL_F = 1;       // Ctrl-F and F3 for Find on Page
            const int HOTKEY_F3 = 2;              // Ctrl-F and F3 for Find on Page
            const int HOTKEY_CONTROL_P = 3;       // Ctrl-P for Print
            const int HOTKEY_CONTROL_R = 4;       // Ctrl-R and F5 for Reload
            const int HOTKEY_F5 = 5;              // Ctrl-R and F5 for Reload
            //const int HOTKEY_CONTROL_PLUS = 6;  // Ctrl-Plus and Ctrl-Minus for zooming
            //const int HOTKEY_CONTROL_MINUS = 7; // Ctrl-Plus and Ctrl-Minus for zooming
            const int HOTKEY_CONTROL_S = 8;       // Ctrl-S for SaveAs
            const int HOTKEY_ALT_LEFTARROW = 9;   // Alt-Left arrow for Back
            const int HOTKEY_ALT_RIGHTARROW = 10; // Alt-Right arrow for Forward

            var hWnd = _window.WindowHandle;

            if (register)
            {
                _ = User32.RegisterHotKey(hWnd, id: HOTKEY_CONTROL_F, User32.KeyModifier.MOD_CONTROL, System.Windows.Forms.Keys.F);
                _ = User32.RegisterHotKey(hWnd, id: HOTKEY_F3, User32.KeyModifier.MOD_NONE, System.Windows.Forms.Keys.F3);
                _ = User32.RegisterHotKey(hWnd, id: HOTKEY_CONTROL_P, User32.KeyModifier.MOD_CONTROL, System.Windows.Forms.Keys.P);
                _ = User32.RegisterHotKey(hWnd, id: HOTKEY_CONTROL_R, User32.KeyModifier.MOD_CONTROL, System.Windows.Forms.Keys.R);
                _ = User32.RegisterHotKey(hWnd, id: HOTKEY_F5, User32.KeyModifier.MOD_NONE, System.Windows.Forms.Keys.F5);
                //_ = User32.RegisterHotKey(hWnd, id: HOTKEY_CONTROL_PLUS, User32.KeyModifier.MOD_CONTROL, System.Windows.Forms.Keys.Add);
                //_ = User32.RegisterHotKey(hWnd, id: HOTKEY_CONTROL_MINUS, User32.KeyModifier.MOD_CONTROL, System.Windows.Forms.Keys.Subtract);
                _ = User32.RegisterHotKey(hWnd, id: HOTKEY_CONTROL_S, User32.KeyModifier.MOD_CONTROL, System.Windows.Forms.Keys.S);
                _ = User32.RegisterHotKey(hWnd, id: HOTKEY_ALT_LEFTARROW, User32.KeyModifier.MOD_ALT, System.Windows.Forms.Keys.Left);
                _ = User32.RegisterHotKey(hWnd, id: HOTKEY_ALT_RIGHTARROW, User32.KeyModifier.MOD_ALT, System.Windows.Forms.Keys.Right);
            }
            else
            {
                _ = User32.UnregisterHotKey(hWnd, id: HOTKEY_CONTROL_F);
                _ = User32.UnregisterHotKey(hWnd, id: HOTKEY_F3);
                _ = User32.UnregisterHotKey(hWnd, id: HOTKEY_CONTROL_P);
                _ = User32.UnregisterHotKey(hWnd, id: HOTKEY_CONTROL_R);
                _ = User32.UnregisterHotKey(hWnd, id: HOTKEY_F5);
                //_ = User32.UnregisterHotKey(hWnd, id: HOTKEY_CONTROL_PLUS);
                //_ = User32.UnregisterHotKey(hWnd, id: HOTKEY_CONTROL_MINUS);
                _ = User32.UnregisterHotKey(hWnd, id: HOTKEY_CONTROL_S);
                _ = User32.UnregisterHotKey(hWnd, id: HOTKEY_ALT_LEFTARROW);
                _ = User32.UnregisterHotKey(hWnd, id: HOTKEY_ALT_RIGHTARROW);
            }
        }

        /// <summary>
        /// Async/non-blocking check for updates
        /// </summary>
        private void CheckForUpdate()
        {
            if (DesktopBridgeHelper.IsPackagedAppInstance)
                return;

            AutoUpdater.AppCastURL = $"https://cdn.sqlbi.com/updates/BravoAutoUpdater.xml?nocache={ DateTimeOffset.Now.ToUnixTimeSeconds() }";
            AutoUpdater.HttpUserAgent = "AutoUpdater";
            AutoUpdater.Synchronous = false;
            AutoUpdater.ShowSkipButton = false;
            AutoUpdater.ShowRemindLaterButton = false;
            AutoUpdater.OpenDownloadPage = true;
            //AutoUpdater.ReportErrors = false;
            //AutoUpdater.RunUpdateAsAdmin = true;
            AutoUpdater.PersistenceProvider = new JsonFilePersistenceProvider(jsonPath: Path.Combine(AppConstants.ApplicationFolderLocalDataPath, "autoupdater.json"));
            AutoUpdater.CheckForUpdateEvent += (updateInfo) =>
            {
                if (updateInfo.Error is not null)
                {
                    TelemetryHelper.TrackException(updateInfo.Error);
                    return;
                }
                
                if (updateInfo.IsUpdateAvailable)
                {
                    var updateMessage = new ApplicationUpdateAvailableWebMessage
                    {
                        DownloadUrl = updateInfo.DownloadURL,
                        ChangelogUrl = updateInfo.ChangelogURL,
                        CurrentVersion = updateInfo.CurrentVersion,
                        InstalledVersion = updateInfo.InstalledVersion.ToString(),
                    };
                    _window.SendWebMessage(updateMessage.AsString);

                    // TODO: complete check for update

                    //var threadStart = new System.Threading.ThreadStart(() => AutoUpdater.ShowUpdateForm(updateInfo));
                    //var thread = new System.Threading.Thread(threadStart);
                    //thread.CurrentCulture = thread.CurrentUICulture = System.Globalization.CultureInfo.CurrentCulture;
                    //thread.SetApartmentState(System.Threading.ApartmentState.STA);
                    //thread.Start();
                    //thread.Join();

                    //NotificationHelper.NotifyUpdateAvailable(updateInfo);
                }
            };
            AutoUpdater.InstalledVersion = new Version(0, 4, 0, 0 /*AppConstants.ApplicationFileVersion*/); // TODO: AutoUpdater version
            AutoUpdater.Start();
        }

        /// <summary>
        /// Starts the native <see cref="PhotinoWindow"/> window that runs the message loop
        /// </summary>
        public void WaitForClose()
        {
            _window.WaitForClose();
        }

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _windowSubclass?.Dispose();
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
