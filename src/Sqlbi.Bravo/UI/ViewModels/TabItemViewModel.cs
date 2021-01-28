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
using Microsoft.Extensions.DependencyInjection;

namespace Sqlbi.Bravo.UI.ViewModels
{
    internal class TabItemViewModel : BaseViewModel
    {
        private BiConnectionType connectionType;

        private readonly IAnalysisServicesEventWatcherService _watcher;
        private readonly IGlobalSettingsProviderService _settings;
        private readonly ILogger _logger;
        private DaxFormatterViewModel _daxFormatterVm;
        private AnalyzeModelViewModel _analyzeModelVm;

        public bool IsRetrying { get; set; } = false;

        public TabItemViewModel(IAnalysisServicesEventWatcherService watcher, ILogger<TabItemViewModel> logger, IGlobalSettingsProviderService settings)
        {
            _logger = logger;
            _watcher = watcher;
            _settings = settings;
            _logger.Trace();
            _watcher.OnConnectionStateChanged += OnAnalysisServicesConnectionStateChanged;

            ShowError = false;

            ConnectionType = BiConnectionType.UnSelected;
            ShowSelectConnection = true;

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
            if (IsRetrying)
                return;

            try
            {
                IsRetrying = true;
                await _callback?.Invoke();

                // As there was no exception assume the retry worked and stop showing the error message.
                ShowError = false;
            }
            catch (Exception exc)
            {
                System.Diagnostics.Debug.WriteLine(exc);
                ErrorDescription = exc.Message;
            }
            finally
            {
                IsRetrying = false;
            }
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

        public DaxFormatterViewModel DaxFormatterVm
        {
            get
            {
                if (_daxFormatterVm == null)
                {
                    _daxFormatterVm = App.ServiceProvider.GetRequiredService<DaxFormatterViewModel>();
                    _daxFormatterVm.ParentTab = this;
                }

                return _daxFormatterVm;
            }
        }

        public AnalyzeModelViewModel AnalyzeModelVm
        {
            get
            {
                if (_analyzeModelVm == null)
                {
                    _analyzeModelVm = App.ServiceProvider.GetRequiredService<AnalyzeModelViewModel>();
                    _analyzeModelVm.ParentTab = this;
                }

                return _analyzeModelVm;
            }
        }

        public bool ShowDaxFormatter
        {
            get => showDaxFormatter;
            set
            {
                if (value)
                {
                    DaxFormatterVm.EnsureInitialized();
                    DoNotShowAnything();
                }

                SetProperty(ref showDaxFormatter, value);
            }
        }

        public bool ShowAnalyzeModel
        {
            get => showAnalyzeModel;
            set
            {
                if (value)
                {
                    AnalyzeModelVm.EnsureInitialized();
                    DoNotShowAnything();
                }

                SetProperty(ref showAnalyzeModel, value);
            }
        }

        public bool ShowSelectConnection
        {
            get => showSelectConnection;
            set
            {
                if (value)
                {
                    DoNotShowAnything();
                }

                SetProperty(ref showSelectConnection, value);
            }
        }

        private void DoNotShowAnything()
        {
            ShowDaxFormatter = false;
            ShowAnalyzeModel = false;
            ShowSelectConnection = false;
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

        public ICommand ConnectCommand { get; set; }

        public ICommand TryAgainCommand { get; set; }

        public bool ShowError { get; set; }

        public RuntimeSummary RuntimeSummary { get; set; } = new RuntimeSummary();

        private Func<Task> _callback;
        private bool showDaxFormatter;
        private bool showSelectConnection;
        private bool showAnalyzeModel;

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

        public void DisplayError(string errorDescription, Func<Task> callback)
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

        internal void ShowSubPage(SubPage? subPageInTab)
        {
            if (subPageInTab == null)
            {
                subPageInTab = SubPage.SelectConnection;
            }

            switch (subPageInTab)
            {
                case SubPage.SelectConnection:
                    ShowSelectConnection = true;
                    break;
                case SubPage.DaxFormatter:
                    ShowDaxFormatter = true;
                    break;
                case SubPage.AnalyzeModel:
                    ShowAnalyzeModel = true;
                    break;
                default:
                    break;
            }
        }
    }
}
