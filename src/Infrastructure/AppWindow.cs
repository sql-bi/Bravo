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
    using Sqlbi.Bravo.Infrastructure.Windows.Interop;
    using Sqlbi.Bravo.Models;
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    internal class AppWindow : IDisposable
    {
        private readonly IHost _host;
        private readonly AppInstance _instance;
        private readonly PhotinoWindow _window;
        private readonly StartupSettings _startupSettings;

        private AppWindowSubclass? _windowSubclass;

        public AppWindow(IHost host, AppInstance instance)
        {
            _host = host;
            _instance = instance;
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
                culture = new
                {
                    IetfLanguageTag = CultureInfo.CurrentCulture.IetfLanguageTag
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

            CommonHelper.CheckForUpdate(UserPreferences.Current.UpdateChannel, updateCallback: (bravoUpdate) =>
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
            _ = Task.Factory.StartNew(() =>
            {
                try
                {
                    Thread.Sleep(2_000);

                    if (!_startupSettings.IsEmpty)
                    {
                        var startupMessage = AppInstanceStartupMessage.CreateFrom(_startupSettings);
                        var startupMessageString = startupMessage.ToWebMessageString();

                        _window.SendWebMessage(startupMessageString);
                    }

                    _instance.OnNewInstance += (sender, arg) =>
                    {
                        if (_window.Minimized)
                        {
                            User32.ShowWindow(_window.WindowHandle, User32.SW_RESTORE);
                        }
                        User32.SetForegroundWindow(_window.WindowHandle);

                        if (arg.Message?.IsEmpty == false)
                        {
                            var webMessageString = arg.Message.ToWebMessageString();
                            _window.SendWebMessage(webMessageString);
                        }
                    };
                }
                catch (Exception ex)
                {
                    TelemetryHelper.TrackException(ex);

                    if (AppEnvironment.IsDiagnosticLevelVerbose)
                        AppEnvironment.AddDiagnostics(name: $"{ nameof(AppWindow) }.{ nameof(WaitForClose) }", ex);
                }
            });
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
