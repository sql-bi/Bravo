using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Core;
using Sqlbi.Bravo.Core.Logging;
using Sqlbi.Bravo.Core.Services;
using Sqlbi.Bravo.Core.Services.Interfaces;
using Sqlbi.Bravo.Core.Settings.Interfaces;
using Sqlbi.Bravo.UI.Framework.ViewModels;
using Sqlbi.Bravo.UI.Views;
using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace Sqlbi.Bravo.UI.ViewModels
{
    internal class ShellViewModel : BaseViewModel
    {
        private readonly IAnalysisServicesEventWatcherService _watcher;
        private readonly ILogger _logger;

        public ShellViewModel(IAnalysisServicesEventWatcherService watcher, ILogger<ShellViewModel> logger)
        {
            _watcher = watcher;
            _logger = logger;

            _logger.Trace();
            _watcher.OnEvent += OnAnalysisServicesEvent;
            _watcher.OnConnectionStateChanged += OnAnalysisServicesConnectionStateChanged;

            PrintDebug();

            Tabs = new ObservableCollection<TabItem>() { TabItem.Create() };
            SelectedTab = Tabs[0];
        }

        public double WindowMinWidth => 800D;

        public double WindowMinHeight => 600D;

        public string WindowTitle => AppConstants.ApplicationNameLabel;

        public ObservableCollection<string> OutputMessages { get; set; } = new ObservableCollection<string>();


        public ObservableCollection<NavigationItem> MenuItems { get; } = new ObservableCollection<NavigationItem>()
        {
            new NavigationItem{ Name = "Format Dax", Glyph = "\uE8A5", NavigationPage = typeof(SelectConnectionType) },
            new NavigationItem{ Name = "Analyze Model", Glyph = "\uE8A5", NavigationPage = null },
            new NavigationItem{ Name = "Manage dates", Glyph = "\uEC92", ShowComingSoon = true },
            new NavigationItem{ Name = "Export data", Glyph = "\uE1AD", ShowComingSoon = true },
            new NavigationItem{ Name = "Best practices", Glyph = "\uE19F", ShowComingSoon = true },
            new NavigationItem{ Name = "Optimize model", Glyph = "\uEC4A", ShowComingSoon = true },
        };

        public ObservableCollection<NavigationItem> OptionMenuItems { get; } = new ObservableCollection<NavigationItem>()
        {
            new NavigationItem{ Name = "Settings", Glyph = "\uE713", NavigationPage = typeof(SettingsViewModel) }
        };

        private ObservableCollection<TabItem> _tabs;

        public ObservableCollection<TabItem> Tabs
        {
            get
            {
                return _tabs;
            }

            set
            {
                SetProperty(ref _tabs, value);
            }
        }

        private TabItem _selectedTab;

        public TabItem SelectedTab
        {
            get
            {
                return _selectedTab;
            }

            set
            {
                SetProperty(ref _selectedTab, value);
            }
        }

        private void OnAnalysisServicesEvent(object sender, AnalysisServicesEventWatcherEventArgs e)
        {
            _logger.Trace();

            var action = new Action<AnalysisServicesEventWatcherEventArgs>((e) =>
            {
                var item = $"OnAnalysisServicesEvent(event<{ e.Event }>)";
                OutputMessages.Add(item);
            });

            Application.Current.Dispatcher.BeginInvoke(action, e);
        }

        [PropertyChanged.SuppressPropertyChangedWarnings]
        private void OnAnalysisServicesConnectionStateChanged(object sender, AnalysisServicesEventWatcherConnectionStateArgs e)
        {
            _logger.Trace();

            var action = new Action<AnalysisServicesEventWatcherConnectionStateArgs>((e) =>
            {
                var item = $"OnAnalysisServicesConnectionStateChanged(current<{ e.Current }>|previous<{ e.Previous }>)";
                OutputMessages.Add(item);
            });

            Application.Current.Dispatcher.BeginInvoke(action, e);
        }

        private void PrintDebug()
        {
            _logger.Trace();

            var settings = App.ServiceProvider.GetService<IGlobalSettingsProviderService>();

            OutputMessages.Add("--- PROCESS INFO ---");
            OutputMessages.Add($"CurrentProcess.Id -> { Environment.ProcessId }");
            OutputMessages.Add($"CurrentProcess.StartTime -> { System.Diagnostics.Process.GetCurrentProcess().StartTime }");
            OutputMessages.Add($"ParentProcess.Id -> { settings.Runtime.ParentProcessId }");
            OutputMessages.Add($"ParentProcess.ProcessName -> { settings.Runtime.ParentProcessName }");
            OutputMessages.Add($"ParentProcess.MainWindowTitle -> { settings.Runtime.ParentProcessMainWindowTitle }");
            OutputMessages.Add($"ParentProcess.MainWindowHandle -> { settings.Runtime.ParentProcessMainWindowHandle }");
            OutputMessages.Add("--- APPLICATION SETTINGS ---");
            OutputMessages.Add($"{ nameof(settings.Application.TelemetryEnabled) } -> { settings.Application.TelemetryEnabled }");
            OutputMessages.Add($"{ nameof(settings.Application.TelemetryLevel) } -> { settings.Application.TelemetryLevel }");
            OutputMessages.Add($"{ nameof(settings.Application.UIShellBringToForegroundOnParentProcessMainWindowScreen) } -> { settings.Application.UIShellBringToForegroundOnParentProcessMainWindowScreen }");
            OutputMessages.Add("--- RUNTIME SETTINGS ---");
            OutputMessages.Add($"{ nameof(settings.Runtime.ServerName) } -> { settings.Runtime.ServerName }");
            OutputMessages.Add($"{ nameof(settings.Runtime.DatabaseName) } -> { settings.Runtime.DatabaseName }");
            OutputMessages.Add($"{ nameof(settings.Runtime.IsExecutedAsExternalTool) } -> { settings.Runtime.IsExecutedAsExternalTool }");
            OutputMessages.Add($"{ nameof(settings.Runtime.IsExecutedAsExternalToolForPowerBIDesktop) } -> { settings.Runtime.IsExecutedAsExternalToolForPowerBIDesktop }");
            OutputMessages.Add($"{ nameof(settings.Runtime.ExternalToolInstanceId) } -> { settings.Runtime.ExternalToolInstanceId }");
            OutputMessages.Add($"{ nameof(settings.Runtime.HasCommandLineParseErrors) } -> { settings.Runtime.HasCommandLineParseErrors }");

            foreach (var error in settings.Runtime.CommandLineParseErrors)
                OutputMessages.Add($"\t{ error }");

            OutputMessages.Add("--- DEBUG ---");
        }
    }
}
