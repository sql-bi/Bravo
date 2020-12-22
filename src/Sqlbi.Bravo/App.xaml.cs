using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Core;
using Sqlbi.Bravo.Core.Logging;
using Sqlbi.Bravo.Core.Services.Interfaces;
using Sqlbi.Bravo.Core.Settings.Interfaces;
using Sqlbi.Bravo.Core.Windows;
using Sqlbi.Bravo.UI.Services.Interfaces;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace Sqlbi.Bravo
{
    public partial class App : Application
    {
        private readonly IHost _host;
        private readonly ILogger _logger;
        private readonly IGlobalSettingsProviderService _settings;

        public static IServiceProvider ServiceProvider { get; private set; }

        public App(IHost host)
        {
            _host = host;

            _settings = _host.Services.GetRequiredService<IGlobalSettingsProviderService>();
            _logger = _host.Services.GetRequiredService<ILogger<App>>();
            _logger.Trace();

            ServiceProvider = _host.Services;

            ConfigureExceptionHandlers();
            ConfigureSingleInstanceOrShutdown();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            _logger.Trace();
            _logger.Information(LogEvents.AppOnStartup);

            var tss = ServiceProvider.GetRequiredService<IThemeSelectorService>();
            tss.InitializeTheme(_settings.Application.ThemeName);

            await _host.StartAsync();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            _logger.Trace();
            _logger.Information(LogEvents.AppOnExit);

            await _host.StopAsync();

            base.OnExit(e);
        }

        private void ConfigureExceptionHandlers()
        {
            _logger.Trace();

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                var exception = e.Exception;
                _logger.Error(LogEvents.TaskSchedulerUnobservedTaskException, exception);
                MessageBox.Show(exception.Message, AppConstants.ApplicationNameLabel, MessageBoxButton.OK, MessageBoxImage.Error);

                // TODO REQUIREMENTS?: SetObserved for UnobservedTaskException
                //e.SetObserved();
            };

            //if (!Debugger.IsAttached)
            {
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    if (e.ExceptionObject is Exception exception)
                    {
                        _logger.Error(LogEvents.AppDomainUnhandledException, exception);
                        MessageBox.Show(exception.Message, AppConstants.ApplicationNameLabel, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };

                Dispatcher.UnhandledException += (s, e) =>
                {
                    var exception = e.Exception;
                    _logger.Error(LogEvents.DispatcherUnhandledException, exception);
                    MessageBox.Show(exception.Message, AppConstants.ApplicationNameLabel, MessageBoxButton.OK, MessageBoxImage.Error);

                    // TODO REQUIREMENTS: Handled for UnhandledException
                    //e.Handled = true;
                };
            }
        }

        private void ConfigureSingleInstanceOrShutdown()
        {
            _logger.Trace();

            if (_settings.Runtime.IsExecutedAsExternalTool)
            {
                var application = ServiceProvider.GetRequiredService<IApplicationInstanceService>();
                if (application.IsCurrentInstanceOwned)
                {
                    application.RegisterCallbackForMultipleInstanceStarted(BringToForeground);
                }
                else
                {
                    _logger.Information(LogEvents.AppShutdownForMultipleInstance);                    
                    Shutdown();
                }
            }
        }

        private void BringToForeground(IntPtr parentProcessMainWindowHandle)
        {
            _logger.Trace();

            Action action = () =>
            {
                var window = Current?.MainWindow;
                if (window == null)
                    return;

                if (window.IsVisible == false)
                    window.Show();

                if (window.WindowState == WindowState.Minimized || window.Visibility == Visibility.Hidden)
                    window.WindowState = WindowState.Normal;

                // According to some sources these steps gurantee that an app will be brought to foreground.
                window.Activate();
                window.Topmost = true;
                window.Topmost = false;
                window.Focus();

                if (_settings.Application.UIShellBringToForegroundOnParentProcessMainWindowScreen)
                {
                    var screen = Screen.FromHandle(parentProcessMainWindowHandle);
                    var screenTop = screen.WorkingArea.Top;
                    var screenLeft = screen.WorkingArea.Left;
                    var screenWidth = screen.WorkingArea.Width;
                    var screenHeight = screen.WorkingArea.Height;
                    window.Top = screenTop + (screenHeight / 2) - (window.Height / 2);
                    window.Left = screenLeft + (screenWidth / 2) - (window.Width / 2);
                }
            };

            Dispatcher.BeginInvoke(action);
        }
    }
}
