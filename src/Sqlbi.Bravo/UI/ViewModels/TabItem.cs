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
    internal class TabItem : BaseViewModel
    {
        private BiConnectionType connectionType;

        private readonly IAnalysisServicesEventWatcherService _watcher;
        private readonly ILogger _logger;

        public TabItem(IAnalysisServicesEventWatcherService watcher, ILogger<SideMenuViewModel> logger)
        {
            _logger = logger;
            _watcher = watcher;

            _logger.Trace();
            _watcher.OnConnectionStateChanged += OnAnalysisServicesConnectionStateChanged;

            ConnectionType = BiConnectionType.UnSelected;
            ContentPageSource = typeof(SelectConnectionType);

            ConnectCommand = new RelayCommand(async () => await Connect());
            DisconnectCommand = new RelayCommand(async () => await Disconnect());
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
                    //   OnPropertyChanged(nameof(Icon));
                }
            }
        }

        public bool IsTabClosable { get; set; } = false;

        public ICommand ConnectCommand { get; set; }

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


        [PropertyChanged.SuppressPropertyChangedWarnings]
        private void OnAnalysisServicesConnectionStateChanged(object sender, AnalysisServicesEventWatcherConnectionStateArgs e)
        {
            _logger.Trace();

            //ConnectCommandIsEnabled = e.Current == ConnectionState.Closed;
            //DisplayViewDaxFormatterCommandIsEnabled = e.Current == ConnectionState.Open;
        }

        public override string ToString()
        {
            return Header;
        }
    }
}
