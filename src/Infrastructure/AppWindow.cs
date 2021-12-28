using Bravo.Infrastructure.Windows.Interop;
using Microsoft.Extensions.Hosting;
using PhotinoNET;
using Sqlbi.Bravo.Infrastructure.Configuration.Options;
using Sqlbi.Bravo.Infrastructure.Extensions;
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
#else
            var contextMenuEnabled = false;
#endif
            var window = new PhotinoWindow()
                .SetTitle(AppConstants.ApplicationMainWindowTitle)
                .SetIconFile("wwwroot/bravo.ico")
                .SetContextMenuEnabled(contextMenuEnabled)
                .SetGrantBrowserPermissions(true)
                .SetUseOsDefaultSize(true)
                .RegisterCustomSchemeHandler("app", CustomSchemeHandler)
                .Load("wwwroot/index.html");

            window.WindowCreating += OnWindowCreating;
            window.WindowCreated += OnWindowCreated;
            window.WindowClosing += OnWindowClosing;
            window.WebMessageReceived += OnWindowWebMessageReceived;
#if DEBUG
            window.SetLogVerbosity(3); // 0 = Critical Only, 1 = Critical and Warning, 2 = Verbose, >2 = All Details. Default is 2.
#else
            window.SetLogVerbosity(1);
#endif
            return window;
        }

        private Stream CustomSchemeHandler(object sender, string scheme, string url, out string contentType)
        {
            contentType = "text/javascript";

            var startupConfig = JsonSerializer.Serialize(new
            {
                address = GetStartupAddress().ToString(),
                theme = GetStartupTheme().ToString()
            });
            var script = $@"var CONFIG = { startupConfig };";
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
                    theme = Win32UxTheme.IsDarkModeEnabled() ? ThemeType.Dark : ThemeType.Light;

                return theme;
            }
        }

        private void OnWindowCreating(object? sender, EventArgs e)
        {
            if (sender is PhotinoWindow window)
            {
                Trace.WriteLine($"::Bravo:INF:OnWindowCreating:{ window.Title }");

                var settings = GetUserSettings();
                if (settings is not null)
                {
                    // Set the preferred theme based on the latest settings saved by the user
                    switch (settings.Theme)
                    {
                        case ThemeType.Light:
                        case ThemeType.Dark:
                            Win32UxTheme.SetStartupMode(useDark: settings.Theme == ThemeType.Dark);
                            break;
                        case ThemeType.Auto:
                            // Let Photino.Native use the OS default
                            break;
                    }
                }
            }
        }

        private void OnWindowCreated(object? sender, EventArgs e)
        {
            if (sender is PhotinoWindow window)
            {
                Trace.WriteLine($"::Bravo:INF:OnWindowCreated:{ window.Title } ( { string.Join(", ", _host.GetListeningAddresses().Select((a) => a.ToString())) } )");
                //window.SendNotification("::Bravo:INF:WindowCreatedHandler", window.Title);

                // Try to install a WndProc subclass callback to hook messages sent to the app main window
                _windowSubclass = new AppWindowSubclass(window);

                // Testing dark theme
                //var task = System.Threading.Tasks.Task.Factory.StartNew(() =>
                //{
                //    bool dark = true;
                //    while (true)
                //    {
                //        System.Threading.Tasks.Task.Delay(3000).Wait();
                //        Win32UxTheme.ChangeTheme(window.WindowHandle, useDark: (dark = !dark));
                //    }
                //});
            }
        }

        private bool OnWindowClosing(object sender, EventArgs args)
        {
            if (sender is PhotinoWindow window)
            {
                Trace.WriteLine($"::Bravo:INF:OnWindowClosing:{ window.Title }");
            }

            return false; // Could return true to stop windows close
        }

        private void OnWindowWebMessageReceived(object? sender, string message)
        {
            Trace.WriteLine($"::Bravo:INF:OnWindowWebMessageReceived:{ message }");

            if (sender is PhotinoWindow window)
            {
                switch (message)
                {
                    case "getStartupConfig":
                        {
                            var responseMessage = JsonSerializer.Serialize(new
                            {
                                getStartupConfig = new
                                {
                                    address = GetStartupAddress().ToString(),
                                    theme = GetStartupTheme().ToString()
                                }
                            });

                            window.SendWebMessage(responseMessage);
                        }
                        break;
                    default:
                        {
                            // TODO: log
                        }
                        break;
                }
            }

            Uri GetStartupAddress()
            {
                var address = _host.GetListeningAddresses().Single(); // single address expected here
                return address;
            }

            ThemeType GetStartupTheme()
            {
                var theme = GetUserSettings()?.Theme ?? ThemeType.Auto;

                if (theme == ThemeType.Auto)
                    theme = Win32UxTheme.IsDarkModeEnabled() ? ThemeType.Dark : ThemeType.Light;

                return theme;
            }
        }

        private UserSettings? GetUserSettings()
        {
            var settingsObject = _host.Services.GetService(typeof(IWritableOptions<UserSettings>)) as IWritableOptions<UserSettings>;
            if (settingsObject?.Value is UserSettings settings)
                return settings;

            return null;
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
