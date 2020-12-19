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

namespace Sqlbi.Bravo.UI.ViewModels
{
    internal class TabItemViewModel : BaseViewModel
    {
        private BiConnectionType connectionType;

        private readonly IAnalysisServicesEventWatcherService _watcher;
        private readonly ILogger _logger;

        public TabItemViewModel(IAnalysisServicesEventWatcherService watcher, ILogger<SideMenuViewModel> logger)
        {
            _logger = logger;
            _watcher = watcher;

            _logger.Trace();
            _watcher.OnConnectionStateChanged += OnAnalysisServicesConnectionStateChanged;

            ShowError = false;

            ConnectionType = BiConnectionType.UnSelected;
            ContentPageSource = typeof(SelectConnectionType);

            ConnectCommand = new RelayCommand(async () => await Connect());
            DisconnectCommand = new RelayCommand(async () => await Disconnect());
            TryAgainCommand = new RelayCommand(async () => await TryAgain());
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

                    case BiConnectionType.DemoMode:
                        return $"Demo data";

                    default: return " ";  // Empty string is treated as null by WinUI control and so shows FullName
                }
            }
        }

        private Type _contentPageSource;

        public Type ContentPageSource
        {
            get
            {
                return _contentPageSource;
            }

            set
            {
                SetProperty(ref _contentPageSource, value);
            }
        }

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

        // TODO: add logic for when all open tabs are closed
        public bool IsTabClosable { get; set; } = true;

        public ICommand ConnectCommand { get; set; }

        public ICommand TryAgainCommand { get; set; }

        public bool ShowError { get; set; }

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
