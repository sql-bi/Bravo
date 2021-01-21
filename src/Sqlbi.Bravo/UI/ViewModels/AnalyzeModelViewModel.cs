using Dax.ViewModel;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Core.Logging;
using Sqlbi.Bravo.Core.Services.Interfaces;
using Sqlbi.Bravo.UI.DataModel;
using Sqlbi.Bravo.UI.Framework.Commands;
using Sqlbi.Bravo.UI.Framework.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Sqlbi.Bravo.UI.ViewModels
{
    internal class AnalyzeModelViewModel : BaseViewModel
    {
        private readonly IAnalyzeModelService _modelService;
        private readonly ILogger _logger;
        internal const int SubViewIndex_Loading = 0;
        internal const int SubViewIndex_Summary = 1;
        internal const int SubViewIndex_Details = 2;
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private List<VpaColumn> _unusedColumns;

        public AnalyzeModelViewModel(IAnalyzeModelService service, ILogger<DaxFormatterViewModel> logger)
        {
            _modelService = service;
            _logger = logger;
            _logger.Trace();
            ViewIndex = SubViewIndex_Loading;

            LoadedCommand = new RelayCommand(execute: async () => await LoadedAsync());
            HelpCommand = new RelayCommand(execute: async () => await ShowHelpAsync());
            ExportVpaxCommand = new RelayCommand(execute: async () => await ExportVpaxAsync());
            RefreshCommand = new RelayCommand(execute: async () => await RefreshAsync());
            MoreDetailsCommand = new RelayCommand(execute: async () => await ShowMoreDetailsAsync());
            BackCommand = new RelayCommand(execute: async () => await BackAsync());

            _timer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 15)
            };
            _timer.Tick += new EventHandler((s, e) => OnPropertyChanged(nameof(TimeSinceLastSync)));
            _timer.Start();
        }

        public TabItemViewModel ParentTab { get; set; }

        public string ConnectionName => ParentTab.ConnectionName;

        public DateTime LastSyncTime => _modelService.GetLastSyncTime();

        public bool LoadOrRefreshCommandIsRunning { get; set; }

        public ICommand LoadedCommand { get; set; }

        public ICommand HelpCommand { get; set; }

        public ICommand ExportVpaxCommand { get; set; }

        public ICommand RefreshCommand { get; set; }

        public ICommand MoreDetailsCommand { get; set; }

        public ICommand BackCommand { get; set; }

        public string LoadingDetails { get; set; }

        public int ViewIndex { get; set; }

        public string DatasetSize { get; set; }

        public int DatasetColumnCount { get; set; }

        public int UnusedColumnCount => UnusedColumns?.Count ?? 0;

        public string UnusedColumnsSize => UnusedColumns?.Sum(c => c.TotalSize).Bytes().ToString("#.#") ?? "-";

        public int TableCount => AllTables?.Count() ?? 0;

        public int AllColumnCount => AllColumns?.Count() ?? 0;

        public int MeasuresCount => AllTables?.Sum(t => t.Measures.Count()) ?? 0;

        public string TotalDbSize => AllTables?.Sum(t => t.TableSize).Bytes().ToString("#.#") ?? "-";

        // TODO REQUIREMENTS: Need to know how to get this value
        public int? MaxRowsCount => null;

        public List<VpaColumn> UnusedColumns
        {
            get => _unusedColumns;
            set
            {
                SetProperty(ref _unusedColumns, value);
                OnPropertyChanged(nameof(UnusedColumnCount));
                OnPropertyChanged(nameof(UnusedColumnsSize));
                OnPropertyChanged(nameof(TableCount));
                OnPropertyChanged(nameof(AllColumnCount));
                OnPropertyChanged(nameof(MeasuresCount));
                OnPropertyChanged(nameof(TotalDbSize));
            }
        }

        public IEnumerable<VpaColumn> AllColumns => _modelService.GetAllColumns();

        public IEnumerable<VpaTable> AllTables => _modelService.GetAllTables();

        public string TimeSinceLastSync
                    => LastSyncTime.Year == 1
                    ? "not yet"
                    : $"{(DateTime.UtcNow - LastSyncTime).Humanize(minUnit: Humanizer.Localisation.TimeUnit.Second).Replace("minute", "min").Replace("second", "sec")} ago";

        private async Task LoadedAsync()
        {
            _logger.Trace();

            try
            {
                await ExecuteCommandAsync(() => LoadOrRefreshCommandIsRunning, InitializeOrRefreshModelAnalyzer);
            }
            catch (Exception exc)
            {
                var shellVm = App.ServiceProvider.GetRequiredService<ShellViewModel>();
                shellVm.SelectedTab.DisplayError($"Unable to connect{Environment.NewLine}{exc.Message}", InitializeOrRefreshModelAnalyzer);
            }
        }

        private async Task RefreshAsync()
        {
            _logger.Trace();

            try
            {
                await ExecuteCommandAsync(() => LoadOrRefreshCommandIsRunning, InitializeOrRefreshModelAnalyzer);
            }
            catch (Exception exc)
            {
                ParentTab.DisplayError($"Unable to connect{Environment.NewLine}{exc.Message}", InitializeOrRefreshModelAnalyzer);
            }
        }

        private async Task ExportVpaxAsync()
        {
            _logger.Trace();

            _ = MessageBox.Show(
                "Need to know what to do here",
                "TODO",
                MessageBoxButton.OK,
                MessageBoxImage.Question);

            await Task.CompletedTask;
        }

        private async Task ShowHelpAsync()
        {
            _logger.Trace();
            await Views.ShellView.Instance.ShowMediaDialog(new HowToAnalyzeModelHelp());
        }

        private async Task ShowMoreDetailsAsync()
        {
            ViewIndex = SubViewIndex_Details;

            await Task.CompletedTask;
        }

        private async Task BackAsync()
        {
            ViewIndex = SubViewIndex_Summary;

            await Task.CompletedTask;
        }

        private async Task InitializeOrRefreshModelAnalyzer()
        {
            LoadingDetails = "Connecting to data";

            await _modelService.InitilizeOrRefreshAsync();

            LoadingDetails = "Analyzing model";
            UpdateSummary();

            OnPropertyChanged(nameof(LastSyncTime));
            OnPropertyChanged(nameof(AllColumns));
            OnPropertyChanged(nameof(AllTables));

            ViewIndex = SubViewIndex_Summary;
        }

        private void UpdateSummary()
        {
            var summary = _modelService.GetDatasetSummary();
            DatasetSize = summary.DatasetSize.Bytes().ToString("#.#");
            DatasetColumnCount = summary.ColumnCount;

            UnusedColumns = _modelService.GetUnusedColumns();
        }
    }
}
