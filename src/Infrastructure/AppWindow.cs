namespace Sqlbi.Bravo.Infrastructure
{
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;
    using PhotinoNET;
    using Sqlbi.Bravo.Infrastructure.Configuration;
    using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Infrastructure.Messages;
    using Sqlbi.Bravo.Models;
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    internal class AppWindow : IDisposable
    {
        private readonly IHost _host;
        private readonly PhotinoWindow _window;
        private readonly StartupSettings _startupSettings;

        private AppWindowSubclass? _windowSubclass;

        public AppWindow(IHost host)
        {
            _host = host;
            _window = CreateWindow();

            var startupSettingsOptions = _host.Services.GetService(typeof(IOptions<StartupSettings>)) as IOptions<StartupSettings>;
            BravoUnexpectedException.ThrowIfNull(startupSettingsOptions);

            _startupSettings = startupSettingsOptions.Value;
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
            var indexHtml = ThemeHelper.ShouldUseDarkMode(UserPreferences.Current.Theme)
                ? "wwwroot/index-dark.html"
                : "wwwroot/index.html";

            var window = new PhotinoWindow()
                .SetIconFile("wwwroot/bravo.ico")
                .SetTitle(AppEnvironment.ApplicationMainWindowTitle)
                .SetTemporaryFilesPath(AppEnvironment.ApplicationTempPath)
                .SetContextMenuEnabled(contextMenuEnabled)
                .SetDevToolsEnabled(devToolsEnabled)
                .SetLogVerbosity(logVerbosity) // 0 = Critical Only, 1 = Critical and Warning, 2 = Verbose, >2 = All Details. Default is 2.
                .SetGrantBrowserPermissions(true)
                .SetUseOsDefaultSize(true)
                .RegisterCustomSchemeHandler("app", CustomSchemeHandler)
                .Load(indexHtml)
                .Center();

            //window.WindowCreating += OnWindowCreating;
            window.WindowCreated += OnWindowCreated;
            window.WindowClosing += OnWindowClosing;

            return window;
        }

        private Stream CustomSchemeHandler(object sender, string scheme, string url, out string contentType)
        {
            contentType = "text/javascript";

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
                telemetry = new
                {
                    instrumentationKey = AppEnvironment.TelemetryInstrumentationKey,
                    contextDeviceOperatingSystem = ContextTelemetryInitializer.DeviceOperatingSystem,
                    contextComponentVersion = ContextTelemetryInitializer.ComponentVersion,
                    contextSessionId = ContextTelemetryInitializer.SessionId,
                    contextUserId = ContextTelemetryInitializer.UserId,
                    globalProperties = ContextTelemetryInitializer.GlobalProperties
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

        //private void OnWindowCreating(object? sender, EventArgs e)
        //{
        //}

        private void OnWindowCreated(object? sender, EventArgs e)
        {
            ThemeHelper.InitializeTheme(_window.WindowHandle, UserPreferences.Current.Theme);
            
            _windowSubclass = AppWindowSubclass.Hook(_window);

            FixStartMenuShortcut();
            CheckForUpdate();
        }

        private bool OnWindowClosing(object sender, EventArgs e)
        {
            NotificationHelper.ClearNotifications();

            // Returning true prevents the window from closing
            return false;
        }

        private static void FixStartMenuShortcut()
        {
            // Every time a Photino application starts up, Photino.Native attempts to creates a shortcut in Windows start menu.
            // This behavior is enabled by default to allow toast notifications because, without a valid shortcut installed, Photino cannot raise a toast notification from a desktop app.
            // If the user has chosen to activate the application shortcut during app installation, this results in a duplicate of the application shortcut in the Windows start menu.
            // The issue has been reported on GitHub, meanwhile let's get rid of the shortcut created by Photino https://github.com/tryphotino/photino.NET/issues/85
            
            var shortcutName = Path.ChangeExtension(AppEnvironment.ApplicationMainWindowTitle, "lnk");
            var shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.DoNotVerify), @"Microsoft\Windows\Start Menu\Programs", shortcutName);
           
            if (File.Exists(shortcutPath))
            {
                try
                {
                    File.Delete(shortcutPath);
                }
                catch (IOException)
                {
                    // ignore "The process cannot access the file '..\Bravo for Power BI.lnk' because it is being used by another process."
                }
            }
        }

        private void CheckForUpdate()
        {
            if (AppEnvironment.IsPackagedAppInstance)
                return;

            CommonHelper.CheckForUpdate(UserPreferences.Current.UpdateChannel, synchronousCallback: false, throwOnError: false, updateCallback: (bravoUpdate) =>
            {
                // HACK: see issue https://github.com/tryphotino/photino.NET/issues/87
                // Wait a bit in order to ensure that the PhotinoWindow message loop is started
                // This is to prevent the .NET Runtime corecrl.dll fault with a win32 access violation
                Thread.Sleep(5_000);
                // HACK END <<

                var updateMessage = ApplicationUpdateAvailableWebMessage.CreateFrom(bravoUpdate);
                _window.SendWebMessage(updateMessage.AsString);

                NotificationHelper.NotifyUpdateAvailable(bravoUpdate);
            });
        }

        /// <summary>
        /// Starts the native <see cref="PhotinoWindow"/> window that runs the message loop
        /// </summary>
        public void WaitForClose()
        {
            // HACK: see issue https://github.com/tryphotino/photino.NET/issues/87
            // Wait a bit in order to ensure that the PhotinoWindow message loop is started
            // This is to prevent the .NET Runtime corecrl.dll fault with a win32 access violation
            // This should be moved to the 'OnWindowCreated' handler after the issue has been resolved
            if (!_startupSettings.IsEmpty)
            {
                _ = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Thread.Sleep(2_000);

                        var startupMessage = AppInstanceStartupMessage.CreateFrom(_startupSettings);
                        var webMessageString = startupMessage.ToWebMessageString();

                        _window.SendWebMessage(webMessageString);
                    }
                    catch
                    {
                        // this is a temporary hack so here we can silently swallow the exception
                    }
                });
            }
            // HACK END <<

            _window.WaitForClose();
        }

        #region IDisposable

        public void Dispose()
        {
            _windowSubclass?.Dispose();
        }

        #endregion
    }
}
