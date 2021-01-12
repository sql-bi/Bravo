using Sqlbi.Bravo.UI.Views;
using Sqlbi.Bravo.UI.Framework.ViewModels;
using System;
using Sqlbi.Bravo.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Core.Logging;
using Sqlbi.Bravo.Core.Services;
using System.Threading.Tasks;
using System.Windows.Input;
using Sqlbi.Bravo.UI.Framework.Commands;
using Sqlbi.Bravo.UI.DataModel;
using Sqlbi.Bravo.Core.Settings.Interfaces;
using Sqlbi.Bravo.Core.Settings;

namespace Sqlbi.Bravo.UI.ViewModels
{
    internal class TabItemViewModel : BaseViewModel
    {
        private BiConnectionType connectionType;

        private readonly IAnalysisServicesEventWatcherService _watcher;
        private readonly IGlobalSettingsProviderService _settings;
        private readonly ILogger _logger;

        public TabItemViewModel(IAnalysisServicesEventWatcherService watcher, ILogger<TabItemViewModel> logger, IGlobalSettingsProviderService settings)
        {
            _logger = logger;
            _watcher = watcher;
            _settings = settings;
            _logger.Trace();
            _watcher.OnConnectionStateChanged += OnAnalysisServicesConnectionStateChanged;

            ShowError = false;

            ConnectionType = BiConnectionType.UnSelected;
            ContentPageSource = typeof(SelectConnectionType);

            ConnectCommand = new RelayCommand(async () => await Connect());
            DisconnectCommand = new RelayCommand(async () => await Disconnect());
            TryAgainCommand = new RelayCommand(async () => await TryAgain());

            // Get the values for the started instance.
            // These will be overridden if messaged to be single instance and open another tab
            RuntimeSummary.DatabaseName = _settings.Runtime.DatabaseName;
            RuntimeSummary.IsExecutedAsExternalTool = _settings.Runtime.IsExecutedAsExternalTool;
            RuntimeSummary.ParentProcessMainWindowTitle = _settings.Runtime.ParentProcessMainWindowTitle;
            RuntimeSummary.ParentProcessName = _settings.Runtime.ParentProcessName;
            RuntimeSummary.ServerName = _settings.Runtime.ServerName;
        }

        private async Task TryAgain()
        {
            ShowError = false;
            _callback?.Invoke();
            await Task.CompletedTask;
        }

        public string Header
        {
            get
            {
                switch (ConnectionType)
                {
                    case BiConnectionType.ConnectedPowerBiDataset:
                        return $"{ConnectionName} - powerbi.com";

                    case BiConnectionType.ActivePowerBiWindow:
                    case BiConnectionType.VertipaqAnalyzerFile:
                        return ConnectionName;

                    default: return " ";  // Empty string is treated as null by WinUI control and so shows FullName
                }
            }
        }

        public Type ContentPageSource { get; set; }

        public string ConnectionName { get; set; }

        public BiConnectionType ConnectionType
        {
            get => connectionType;
            set
            {
                if (SetProperty(ref connectionType, value))
                {
                    OnPropertyChanged(nameof(Header));
                }
            }
        }

        public ICommand ConnectCommand { get; set; }

        public ICommand TryAgainCommand { get; set; }

        public bool ShowError { get; set; }

        public RuntimeSummary RuntimeSummary { get; set; } = new RuntimeSummary();

        private Action _callback;

        public string ErrorDescription { get; set; }

        public ICommand DisconnectCommand { get; set; }

        public bool ConnectCommandIsRunning { get; set; }

        public bool DisconnectCommandIsRunning { get; set; }

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

        public void DisplayError(string errorDescription, Action callback)
        {
            ErrorDescription = errorDescription;
            ShowError = true;
            _callback = callback;
        }

        [PropertyChanged.SuppressPropertyChangedWarnings]
        private void OnAnalysisServicesConnectionStateChanged(object sender, AnalysisServicesEventWatcherConnectionStateArgs e)
        {
            _logger.Trace();

            //ConnectCommandIsEnabled = e.Current == ConnectionState.Closed;
            //DisplayViewDaxFormatterCommandIsEnabled = e.Current == ConnectionState.Open;
        }

        public override string ToString() => Header;
    }
}
