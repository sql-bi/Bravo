using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Core.Logging;
using Sqlbi.Bravo.Core.Settings;
using Sqlbi.Bravo.Core.Settings.Interfaces;
using Sqlbi.Bravo.UI.DataModel;
using Sqlbi.Bravo.UI.Framework.Commands;
using Sqlbi.Bravo.UI.Framework.ViewModels;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Sqlbi.Bravo.UI.ViewModels
{
    internal class TabItemViewModel : BaseViewModel
    {
        private readonly IGlobalSettingsProviderService _settings;
        private readonly ILogger _logger;
        private DaxFormatterViewModel _daxFormatterVm;
        private AnalyzeModelViewModel _analyzeModelVm;
        private BiConnectionType _connectionType;
        private Func<Task> _tryagainCallback;
        private bool _showDaxFormatter;
        private bool _showSelectConnection;
        private bool _showAnalyzeModel;

        public bool IsRetrying { get; set; } = false;

        public TabItemViewModel(ILogger<TabItemViewModel> logger, IGlobalSettingsProviderService settings)
        {
            _logger = logger;
            _settings = settings;

            _logger.Trace();

            ShowError = false;
            ConnectionType = BiConnectionType.UnSelected;
            ShowSelectConnection = true;

            TryAgainCommand = new RelayCommand(async () => await TryAgain());
        }

        private async Task TryAgain()
        {
            if (IsRetrying)
            {
                return;
            }

            try
            {
                IsRetrying = true;

                await _tryagainCallback?.Invoke();

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
                    default: 
                        return " ";  // Empty string is treated as null by WinUI control and so shows FullName
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
            get => _showDaxFormatter;
            set
            {
                if (value)
                {
                    DaxFormatterVm.EnsureInitialized();
                    DoNotShowAnything();
                }

                SetProperty(ref _showDaxFormatter, value);
            }
        }

        public bool ShowAnalyzeModel
        {
            get => _showAnalyzeModel;
            set
            {
                if (value)
                {
                    AnalyzeModelVm.EnsureInitialized();
                    DoNotShowAnything();
                }

                SetProperty(ref _showAnalyzeModel, value);
            }
        }

        public bool ShowSelectConnection
        {
            get => _showSelectConnection;
            set
            {
                if (value)
                {
                    DoNotShowAnything();
                }

                SetProperty(ref _showSelectConnection, value);
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
            get => _connectionType;
            set
            {
                if (SetProperty(ref _connectionType, value))
                {
                    OnPropertyChanged(nameof(Header));
                }
            }
        }

        public ICommand TryAgainCommand { get; set; }

        public bool ShowError { get; set; }

        public ConnectionSettings ConnectionSettings { get; set; }

        public string ErrorDescription { get; set; }

        public void DisplayError(string errorDescription, Func<Task> callback)
        {
            ErrorDescription = errorDescription;
            ShowError = true;
            _tryagainCallback = callback;
        }

        public override string ToString() => Header;

        internal void ShowSubPage(SubPage? subPageInTab)
        {
            if (subPageInTab == null)
            {
                subPageInTab = SubPage.SelectConnection;
            }

            var shellViewModel = App.ServiceProvider.GetRequiredService<ShellViewModel>();

            switch (subPageInTab)
            {
                case SubPage.SelectConnection:
                    ShowSelectConnection = true;
                    break;
                case SubPage.DaxFormatter:
                    ShowDaxFormatter = true;
                    shellViewModel.SelectedIndex = ShellViewModel.MenuItemFormatDaxIndex;
                    break;
                case SubPage.AnalyzeModel:
                    ShowAnalyzeModel = true;
                    shellViewModel.SelectedIndex = ShellViewModel.MenuItemAnalyzeModelIndex;
                    break;
            }
        }
    }
}
