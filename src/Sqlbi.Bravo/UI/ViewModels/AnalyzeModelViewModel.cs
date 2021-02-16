using Dax.Metadata;
using Dax.ViewModel;
using Dax.Vpax.Tools;
using Humanizer;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Sqlbi.Bravo.Core.Logging;
using Sqlbi.Bravo.Core.Services.Interfaces;
using Sqlbi.Bravo.UI.DataModel;
using Sqlbi.Bravo.UI.Framework.Commands;
using Sqlbi.Bravo.UI.Framework.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly List<VpaTableColumnViewModel> _allTablesCache = new List<VpaTableColumnViewModel>();
        private List<VpaColumn> _unusedColumns;
        private bool _initialized = false;

        public AnalyzeModelViewModel(IAnalyzeModelService service, ILogger<DaxFormatterViewModel> logger)
        {
            _modelService = service;
            _logger = logger;
            _logger.Trace();
            ViewIndex = SubViewIndex_Loading;

            HelpCommand = new RelayCommand(execute: () => ShowHelp());
            ExportVpaxCommand = new RelayCommand(execute: async () => await ExportVpaxAsync());
            RefreshCommand = new RelayCommand(execute: async () => await RefreshAsync());
            MoreDetailsCommand = new RelayCommand(execute: async () => await ShowMoreDetailsAsync());
            BackCommand = new RelayCommand(execute: async () => await BackAsync());
            WarningHelpCommand = new RelayCommand(execute: () => ShowWarningHelp());

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

        internal void OverrideDaxModel(Model daxModel)
        {
            _modelService.OverrideDaxModel(daxModel);
        }

        public ICommand HelpCommand { get; set; }

        public ICommand WarningHelpCommand { get; set; }

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

        public long MaxRowsCount => AllTables?.Max(t => t.RowsCount) ?? 0;

        public long LargestColumnSize { get; set; }

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
                OnPropertyChanged(nameof(MaxRowsCount));
            }
        }

        public IEnumerable<VpaTableColumnViewModel> AllTableColumns
        {
            get
            {
                if (!_allTablesCache.Any())
                {
                    VpaTableColumnViewModel currentTable = null;

                    var cols = _modelService.GetAllColumns();

                    if (cols != null)
                    {
                        foreach (var col in cols)
                        {
                            if (currentTable == null || currentTable.TableName != col.Table.TableName)
                            {
                                currentTable = new VpaTableColumnViewModel(this, col, true)
                                {
                                    TotalSize = col.Table.ColumnsTotalSize,
                                    PercentageDatabase = col.Table.PercentageDatabase
                                };

                                _allTablesCache.Add(currentTable);
                            }
                            else
                            {
                                // Table doesn't expose this so have to sum it manually.
                                currentTable.Cardinality += col.ColumnCardinality;

                                currentTable.Columns.Add(new VpaTableColumnViewModel(this, col, false));
                            }
                        }
                    }
                }

                return _allTablesCache;
            }
        }

        internal void EnsureInitialized()
        {
            if (!_initialized)
            {
                Task.Run(async () => await RefreshAsync());
            }
        }

        public IEnumerable<VpaColumnViewModel> AllColumns => AllTableColumns?.SelectMany(t => t.Columns);

        public IEnumerable<VpaTable> AllTables => _modelService.GetAllTables();

        public int? SelectedColumnCount => AllColumns?.Count(c => c.IsSelected) ?? 0;

        public long? SelectedColumnSize => AllColumns?.Where(c => c.IsSelected).Sum(c => c.TotalSize);

        public double? SelectedColumnWeight => AllColumns?.Where(c => c.IsSelected).Sum(c => c.PercentageDatabase);

        public string TimeSinceLastSync
                    => LastSyncTime.Year == 1
                    ? "not yet"
                    : $"{(DateTime.UtcNow - LastSyncTime).Humanize(minUnit: Humanizer.Localisation.TimeUnit.Second).Replace("minute", "min").Replace("second", "sec")} ago";

        private async Task RefreshAsync()
        {
            _logger.Trace();

            try
            {
                var lastIndex = ViewIndex;

                try
                {
                    ViewIndex = SubViewIndex_Loading;

                    await ExecuteCommandAsync(() => LoadOrRefreshCommandIsRunning, InitializeOrRefreshModelAnalyzer);
                }
                finally
                {
                    ViewIndex = lastIndex > 0 ? lastIndex : SubViewIndex_Summary;
                }
            }
            catch (Exception exc)
            {
                ParentTab.DisplayError($"Unable to connect{Environment.NewLine}{exc.Message}", InitializeOrRefreshModelAnalyzer);
            }
        }

        private async Task ExportVpaxAsync()
        {
            _logger.Trace();

            var saveFileDialog = new SaveFileDialog
            {
                FileName = $"{ParentTab.ConnectionName}.vpax",
                Filter = "VPAX (*.vpax)|*.vpax",
                DefaultExt = ".vpax",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                VpaxTools.ExportVpax(saveFileDialog.FileName, _modelService.GetModelForExport());
            }

            await Task.CompletedTask;
        }

        private void ShowHelp()
        {
            _logger.Trace();
            Views.ShellView.Instance.ShowMediaDialog(new HowToAnalyzeModelHelp());
        }

        private void ShowWarningHelp()
        {
            _logger.Trace();
            Views.ShellView.Instance.ShowMediaDialog(new ColumnOptimizationHelp());
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

            await _modelService.InitilizeOrRefreshAsync(ParentTab.RuntimeSummary);

            LoadingDetails = "Analyzing model";

            await Task.Run(() => UpdateSummary());

            OnPropertyChanged(nameof(TimeSinceLastSync));
            OnPropertyChanged(nameof(AllTables));
            OnPropertyChanged(nameof(AllTableColumns));
            OnPropertyChanged(nameof(AllColumns));
            OnPropertyChanged(nameof(AllColumnCount));

            _initialized = true;
        }

        private void UpdateSummary()
        {
            _allTablesCache.Clear();
            var summary = _modelService.GetDatasetSummary();
            DatasetSize = summary.DatasetSize.Bytes().ToString("#.#");
            DatasetColumnCount = summary.ColumnCount;
            LargestColumnSize = AllColumns?.Any() ?? false ? AllColumns?.Max(c => c.TotalSize) ?? 0 : 0;

            // Sort these here so the DataGrid doesn't have to worry about loading all rows to be able to sort
            UnusedColumns = _modelService.GetUnusedColumns().OrderByDescending(c => c.PercentageDatabase).ToList();
        }

        internal System.Windows.Media.Color GetTableColor(string tableName)
        {
            var orderedTables = AllTableColumns.OrderByDescending(t => t.TotalSize).ToList();

            // TODO REQUIREMENTS: Need to define the actual colors to use.
            switch (orderedTables.FindIndex(t => t.TableName.Equals(tableName)))
            {
                case 0:
                    return System.Windows.Media.Colors.LightBlue;
                case 1:
                    return System.Windows.Media.Colors.LightGreen;
                case 2:
                    return System.Windows.Media.Colors.LightPink;
                case 3:
                    return System.Windows.Media.Colors.LightYellow;
                case 4:
                    return System.Windows.Media.Colors.LightSlateGray;
                case 5:
                    return System.Windows.Media.Colors.LightSteelBlue;
                case 6:
                    return System.Windows.Media.Colors.LightCyan;
                default:
                    return System.Windows.Media.Colors.Red;  // Default
            }
        }
    }
}
