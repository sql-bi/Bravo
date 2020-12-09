using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Core.Client.Http;
using Sqlbi.Bravo.Core.Helpers;
using Sqlbi.Bravo.Core.Logging;
using Sqlbi.Bravo.Core.Services;
using Sqlbi.Bravo.Core.Services.Interfaces;
using Sqlbi.Bravo.UI.Framework.Commands;
using Sqlbi.Bravo.UI.Framework.Helpers;
using Sqlbi.Bravo.UI.Framework.ViewModels;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Sqlbi.Bravo.UI.ViewModels
{
    internal class DaxFormatterViewModel : BaseViewModel
    {
        private readonly IDaxFormatterService _formatter;
        private readonly IAnalysisServicesEventWatcherService _watcher;
        private readonly ILogger _logger;

        public DaxFormatterViewModel(IDaxFormatterService formatter, IAnalysisServicesEventWatcherService watcher, ILogger<DaxFormatterViewModel> logger)
        {
            _formatter = formatter;
            _watcher = watcher;
            _logger = logger;

            _logger.Trace();
            _watcher.OnEvent += OnAnalysisServicesEvent;
            //_watcher.OnConnectionStateChanged += OnAnalysisServicesConnectionStateChanged;

            LoadedCommand = new RelayCommand(execute: async () => await LoadedAsync());

            InitializeCommand = new RelayCommand(execute: async () => await InitializeAsync());
            FormatCommand = new RelayCommand(execute: async () => await FormatAsync(), canExecute: () => !InitializeCommandIsEnabled && TabularObjectType != DaxFormatterTabularObjectType.None).ObserveProperties(this, nameof(TabularObjectType), nameof(InitializeCommandIsEnabled));
        }

        private DaxFormatterTabularObjectType TabularObjectType { get; set; }  = DaxFormatterTabularObjectType.None;

        public ICommand LoadedCommand { get; set; }

        public ICommand InitializeCommand { get; set; }

        public bool InitializeCommandIsRunning { get; set; }

        public bool InitializeCommandIsEnabled { get; set; } = true;

        public ICommand FormatCommand { get; set; }

        public bool FormatCommandIsRunning { get; set; }

        public bool FormatCommandIsEnabled { get; set; }

        public bool TabularObjectMeasuresIsChecked
        { 
            get => TabularObjectType.HasFlag(DaxFormatterTabularObjectType.Measures);
            set => TabularObjectType = TabularObjectType.WithFlag(DaxFormatterTabularObjectType.Measures, set: value);
        }

        public int TabularObjectMeasuresCount { get; set; }

        public bool TabularObjectCalculatedColumnsIsChecked
        {
            get => TabularObjectType.HasFlag(DaxFormatterTabularObjectType.CalculatedColumns);
            set => TabularObjectType = TabularObjectType.WithFlag(DaxFormatterTabularObjectType.CalculatedColumns, set: value);
        }

        public int TabularObjectCalculatedColumnsCount { get; set; }

        public bool TabularObjectKPIsIsChecked
        {
            get => TabularObjectType.HasFlag(DaxFormatterTabularObjectType.KPIs);
            set => TabularObjectType = TabularObjectType.WithFlag(DaxFormatterTabularObjectType.KPIs, set: value);
        }

        public int TabularObjectKPIsCount { get; set; }

        public bool TabularObjectDetailRowsDefinitionsIsChecked
        {
            get => TabularObjectType.HasFlag(DaxFormatterTabularObjectType.DetailRowsDefinitions);
            set => TabularObjectType = TabularObjectType.WithFlag(DaxFormatterTabularObjectType.DetailRowsDefinitions, set: value);
        }

        public int TabularObjectDetailRowsDefinitionsCount { get; set; }

        public bool TabularObjectCalculationItemsIsChecked
        {
            get => TabularObjectType.HasFlag(DaxFormatterTabularObjectType.CalculationItems);
            set => TabularObjectType = TabularObjectType.WithFlag(DaxFormatterTabularObjectType.CalculationItems, set: value);
        }

        public int TabularObjectCalculationItemsCount { get; set; }

        private async void OnAnalysisServicesEvent(object sender, AnalysisServicesEventWatcherEventArgs e)
        {
            _logger.Trace();

            if (e.Event == AnalysisServicesEventWatcherEvent.Create ||
                e.Event == AnalysisServicesEventWatcherEvent.Alter ||
                e.Event == AnalysisServicesEventWatcherEvent.Delete)
            {
                await InitializeOrRefreshFormatter();
            }
        }

        private async Task InitializeOrRefreshFormatter()
        {
            FormatCommandIsEnabled = false;
            await _formatter.InitilizeOrRefreshAsync();

            TabularObjectMeasuresCount = _formatter.Count(DaxFormatterTabularObjectType.Measures);
            TabularObjectCalculatedColumnsCount = _formatter.Count(DaxFormatterTabularObjectType.CalculatedColumns);
            TabularObjectKPIsCount = _formatter.Count(DaxFormatterTabularObjectType.KPIs);
            TabularObjectDetailRowsDefinitionsCount = _formatter.Count(DaxFormatterTabularObjectType.DetailRowsDefinitions);
            TabularObjectCalculationItemsCount = _formatter.Count(DaxFormatterTabularObjectType.CalculationItems);

            FormatCommandIsEnabled = true;
            InitializeCommandIsEnabled = false;
        }

        private async Task LoadedAsync()
        {
            _logger.Trace();

            await ExecuteCommandAsync(() => InitializeCommandIsRunning, InitializeOrRefreshFormatter);
        }

        private async Task InitializeAsync()
        {
            _logger.Trace();

            await ExecuteCommandAsync(() => InitializeCommandIsRunning, InitializeOrRefreshFormatter);
        }

        private async Task FormatAsync()
        {
            _logger.Trace();

            await ExecuteCommandAsync(() => FormatCommandIsRunning, async () => await _formatter.FormatAsync(TabularObjectType));
        }
    }
}
