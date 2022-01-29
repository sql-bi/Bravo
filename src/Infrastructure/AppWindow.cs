namespace Sqlbi.Bravo.Infrastructure
{
    using AutoUpdaterDotNET;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;
    using PhotinoNET;
    using Sqlbi.Bravo.Infrastructure.Configuration;
    using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Infrastructure.Messages;
    using Sqlbi.Bravo.Infrastructure.Windows.Interop;
    using Sqlbi.Bravo.Models;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Json;

    internal class AppWindow : IDisposable
    {
        private readonly IHost _host;
        private readonly PhotinoWindow _window;

        private AppWindowSubclass? _windowSubclass;
        private bool _disposed;

        public AppWindow(IHost host)
        {
            _host = host;
            _window = CreateWindow();
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
                .SetTemporaryFilesPath(AppConstants.ApplicationTempPath)
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
                token = AppConstants.ApiAuthenticationToken,
                address = GetAddress().ToString(),
                version = AppConstants.ApplicationProductVersion,
                build = AppConstants.ApplicationFileVersion,
                options = BravoOptions.CreateFromUserPreferences(),
                telemetry = new
                {
                    instrumentationKey = AppConstants.TelemetryInstrumentationKey,
                    contextDeviceOperatingSystem = ContextTelemetryInitializer.DeviceOperatingSystem,
                    contextComponentVersion = ContextTelemetryInitializer.ComponentVersion,
                    contextSessionId = ContextTelemetryInitializer.SessionId,
                    contextUserId = ContextTelemetryInitializer.UserId,
                    globalProperties = ContextTelemetryInitializer.GlobalProperties
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
                var theme = UserPreferences.Current.Theme;

                if (theme == ThemeType.Auto)
                    theme = Uxtheme.IsSystemUsingDarkMode() ? ThemeType.Dark : ThemeType.Light;

                return theme;
            }
        }

        private void OnWindowCreating(object? sender, EventArgs e)
        {
            Trace.WriteLine($"::Bravo:INF:OnWindowCreating:{ _window.Title }");

            var theme = UserPreferences.Current.Theme;
            if (theme != ThemeType.Auto) 
            {
                // Set the startup theme based on the latest settings saved by the user
                Uxtheme.SetStartupTheme(useDark: theme == ThemeType.Dark);
            }
        }

        private void OnWindowCreated(object? sender, EventArgs e)
        {
            Trace.WriteLine($"::Bravo:INF:OnWindowCreated:{ _window.Title } ( { string.Join(", ", _host.GetListeningAddresses().Select((a) => a.ToString())) } )");
#if !DEBUG
            HandleHotKeys(register: true);
#endif
            _windowSubclass = AppWindowSubclass.Hook(_window);

            // Every time a Photino application starts up, Photino.Native attempts to creates a shortcut in Windows start menu.
            // This behavior is enabled by default to allow toast notifications because, without a valid shortcut installed, Photino cannot raise a toast notification from a desktop app.
            // If the user has chosen to activate the application shortcut during app installation, this results in a duplicate of the application shortcut in the Windows start menu.
            // The issue has been reported on GitHub, meanwhile let's get rid of the shortcut created by Photino https://github.com/tryphotino/photino.NET/issues/85
            var shortcutName = Path.ChangeExtension(AppConstants.ApplicationMainWindowTitle, "lnk");
            var shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.DoNotVerify), @"Microsoft\Windows\Start Menu\Programs", shortcutName);
            if (File.Exists(shortcutPath))
                File.Delete(shortcutPath);

            CheckForUpdate();
        }

        private bool OnWindowClosing(object sender, EventArgs e)
        {
            NotificationHelper.ClearNotifications();
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
                _ = User32.RegisterHotKey(hWnd, id: HOTKEY_CONTROL_F, User32.MODEKEY.MOD_CONTROL, System.Windows.Forms.Keys.F);
                _ = User32.RegisterHotKey(hWnd, id: HOTKEY_F3, User32.MODEKEY.MOD_NONE, System.Windows.Forms.Keys.F3);
                _ = User32.RegisterHotKey(hWnd, id: HOTKEY_CONTROL_P, User32.MODEKEY.MOD_CONTROL, System.Windows.Forms.Keys.P);
                _ = User32.RegisterHotKey(hWnd, id: HOTKEY_CONTROL_R, User32.MODEKEY.MOD_CONTROL, System.Windows.Forms.Keys.R);
                _ = User32.RegisterHotKey(hWnd, id: HOTKEY_F5, User32.MODEKEY.MOD_NONE, System.Windows.Forms.Keys.F5);
                //_ = User32.RegisterHotKey(hWnd, id: HOTKEY_CONTROL_PLUS, User32.KeyModifier.MOD_CONTROL, System.Windows.Forms.Keys.Add);
                //_ = User32.RegisterHotKey(hWnd, id: HOTKEY_CONTROL_MINUS, User32.KeyModifier.MOD_CONTROL, System.Windows.Forms.Keys.Subtract);
                _ = User32.RegisterHotKey(hWnd, id: HOTKEY_CONTROL_S, User32.MODEKEY.MOD_CONTROL, System.Windows.Forms.Keys.S);
                _ = User32.RegisterHotKey(hWnd, id: HOTKEY_ALT_LEFTARROW, User32.MODEKEY.MOD_ALT, System.Windows.Forms.Keys.Left);
                _ = User32.RegisterHotKey(hWnd, id: HOTKEY_ALT_RIGHTARROW, User32.MODEKEY.MOD_ALT, System.Windows.Forms.Keys.Right);
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
            if (AppConstants.IsPackagedAppInstance)
                return;

            AutoUpdater.AppCastURL = string.Format("https://cdn.sqlbi.com/updates/BravoAutoUpdater.xml?nocache={0}", DateTimeOffset.Now.ToUnixTimeSeconds());
            AutoUpdater.HttpUserAgent = "AutoUpdater";
            AutoUpdater.Synchronous = false;
            AutoUpdater.ShowSkipButton = false;
            AutoUpdater.ShowRemindLaterButton = false;
            AutoUpdater.OpenDownloadPage = false;
            AutoUpdater.PersistenceProvider = new JsonFilePersistenceProvider(jsonPath: Path.Combine(AppConstants.ApplicationDataPath, "autoupdater.json"));
            AutoUpdater.CheckForUpdateEvent += (updateInfo) =>
            {
                if (updateInfo.Error is not null)
                {
                    TelemetryHelper.TrackException(updateInfo.Error);
                }
                else if (updateInfo.IsUpdateAvailable)
                {
                    var updateMessage = ApplicationUpdateAvailableWebMessage.CreateFrom(updateInfo);
                    _window.SendWebMessage(updateMessage.AsString);

                    NotificationHelper.NotifyUpdateAvailable(updateInfo);
                }
            };
            AutoUpdater.InstalledVersion = Version.Parse(AppConstants.ApplicationFileVersion);
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
