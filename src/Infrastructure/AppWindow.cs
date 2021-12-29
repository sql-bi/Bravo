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
#if DEBUG || DEBUG_WWWROOT
            var contextMenuEnabled = true;
            var logVerbosity = 3;
#else
            var contextMenuEnabled = false;
            var logVerbosity = 1;
#endif
            var window = new PhotinoWindow()
                .SetTitle(AppConstants.ApplicationMainWindowTitle)
                .SetIconFile("wwwroot/bravo.ico")
                .SetContextMenuEnabled(contextMenuEnabled)
                .SetLogVerbosity(logVerbosity) // 0 = Critical Only, 1 = Critical and Warning, 2 = Verbose, >2 = All Details. Default is 2.
                .SetGrantBrowserPermissions(true)
                .SetUseOsDefaultSize(true)
                .RegisterCustomSchemeHandler("app", CustomSchemeHandler)
                .Load("wwwroot/index.html");

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
                    theme = Win32UxTheme.IsSystemUsingDarkMode() ? ThemeType.Dark : ThemeType.Light;

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
                    Win32UxTheme.SetStartupTheme(useDark: settings.Theme == ThemeType.Dark);
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
