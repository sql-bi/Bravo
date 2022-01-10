using AutoUpdaterDotNET;
using Bravo.Infrastructure.Windows.Interop;
using Microsoft.Extensions.Hosting;
using PhotinoNET;
using Sqlbi.Bravo.Infrastructure.Configuration.Options;
using Sqlbi.Bravo.Infrastructure.Extensions;
using Sqlbi.Bravo.Infrastructure.Helpers;
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

            return window;
        }

        private Stream CustomSchemeHandler(object sender, string scheme, string url, out string contentType)
        {
            contentType = "text/javascript";

            var config = JsonSerializer.Serialize(new
            {
                address = GetStartupAddress().ToString(),
                theme = GetStartupTheme().ToString()
            });
            var script = $@"var CONFIG = { config };";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(script));

            return stream;

            Uri GetStartupAddress()
            {
                var address = _host.GetListeningAddresses().Single(); // single address expected here
                return address;
            }

            ThemeType GetStartupTheme()
            {
                var theme = GetUserSettings()?.Theme ?? ThemeType.Auto;

                if (theme == ThemeType.Auto)
                    theme = Uxtheme.IsSystemUsingDarkMode() ? ThemeType.Dark : ThemeType.Light;

                return theme;
            }
        }

        private void OnWindowCreating(object? sender, EventArgs e)
        {
            if (sender is PhotinoWindow window)
            {
                Trace.WriteLine($"::Bravo:INF:OnWindowCreating:{ window.Title }");

                var settings = GetUserSettings();
                if (settings is not null && settings.Theme != ThemeType.Auto) 
                {
                    // Set the startup theme based on the latest settings saved by the user
                    Uxtheme.SetStartupTheme(useDark: settings.Theme == ThemeType.Dark);
                }
            }
        }

        private void OnWindowCreated(object? sender, EventArgs e)
        {
            if (sender is PhotinoWindow window)
            {
                Trace.WriteLine($"::Bravo:INF:OnWindowCreated:{ window.Title } ( { string.Join(", ", _host.GetListeningAddresses().Select((a) => a.ToString())) } )");

                // Try to install a WndProc subclass callback to hook messages sent to the app main window
                _windowSubclass = new AppWindowSubclass(window);

                // Async/non-blocking check for updates
                CheckForUpdate();
            }
        }

        private UserSettings? GetUserSettings()
        {
            var settingsObject = _host.Services.GetService(typeof(IWritableOptions<UserSettings>)) as IWritableOptions<UserSettings>;
            if (settingsObject?.Value is UserSettings settings)
                return settings;

            return null;
        }

        private static void CheckForUpdate()
        {
            if (DesktopBridgeHelpers.IsPackagedAppInstance)
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
                    // TODO: complete check for update

                    //var threadStart = new ThreadStart(() => AutoUpdater.ShowUpdateForm(update));
                    //var thread = new Thread(threadStart);
                    //thread.CurrentUICulture = thread.CurrentUICulture = CultureInfo.CurrentCulture;
                    //thread.SetApartmentState(ApartmentState.STA);
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
