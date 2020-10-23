using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Core.Logging;
using Sqlbi.Bravo.Core.Services;
using Sqlbi.Bravo.Core.Services.Interfaces;
using Sqlbi.Bravo.UI.DataModel;
using Sqlbi.Bravo.UI.Framework.Commands;
using Sqlbi.Bravo.UI.Framework.ViewModels;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Sqlbi.Bravo.UI.ViewModels
{
    internal class SideMenuViewModel : BaseViewModel
    {
        private readonly IAnalysisServicesEventWatcherService _watcher;
        private readonly ILogger _logger;

        public SideMenuViewModel(IAnalysisServicesEventWatcherService watcher, ILogger<SideMenuViewModel> logger)
        {
            _logger = logger;
            _watcher = watcher;

            _logger.Trace();
            _watcher.OnConnectionStateChanged += OnAnalysisServicesConnectionStateChanged;

            ConnectCommand = new RelayCommand(async () => await Connect());
            DisconnectCommand = new RelayCommand(async () => await Disconnect());
            CloseCommand = new RelayCommand(async () => await Close());
            DisplayViewSettingsCommand = new RelayCommand(async () => await DisplayViewSettings());
            DisplayViewDaxFormatterCommand = new RelayCommand(async () => await DisplayViewDaxFormatter());
        }

        public ICommand ConnectCommand { get; set; }

        public ICommand DisconnectCommand { get; set; }

        public ICommand CloseCommand { get; set; }

        public ICommand DisplayViewSettingsCommand { get; set; }

        public ICommand DisplayViewDaxFormatterCommand { get; set; }

        public bool ConnectCommandIsRunning { get; set; }

        public bool ConnectCommandIsEnabled { get; set; } = true;

        public bool DisconnectCommandIsRunning { get; set; }

        public bool CloseCommandIsRunning { get; set; }

        public bool DisplayViewSettingsCommandIsEnabled { get; set; } = true;

        public bool DisplayViewDaxFormatterCommandIsEnabled { get; set; }

        private async Task Connect()
        {
            _logger.Trace();

            await ExecuteCommandAsync(() => ConnectCommandIsRunning, _watcher.ConnectAsync);
        }

        private async Task Disconnect()
        {
            _logger.Trace();

            await ExecuteCommandAsync(() => DisconnectCommandIsRunning, _watcher.DisconnectAsync);
        }

        private async Task Close()
        {
            _logger.Trace();

            await ExecuteCommandAsync(() => CloseCommandIsRunning, _watcher.DisconnectAsync);
            Application.Current.Shutdown();
        }

        private async Task DisplayViewSettings()
        {
            _logger.Trace();

            ApplicationViewModel.Instance.CurrentView = ApplicationView.Settings;
            await Task.CompletedTask;
        }

        private async Task DisplayViewDaxFormatter()
        {
            _logger.Trace();

            ApplicationViewModel.Instance.CurrentView = ApplicationView.DaxFormatter;
            await Task.CompletedTask;
        }

        [PropertyChanged.SuppressPropertyChangedWarnings]
        private void OnAnalysisServicesConnectionStateChanged(object sender, AnalysisServicesEventWatcherConnectionStateArgs e)
        {
            _logger.Trace();

            ConnectCommandIsEnabled = e.Current == ConnectionState.Closed;
            DisplayViewDaxFormatterCommandIsEnabled = e.Current == ConnectionState.Open;
        }
    }
}
