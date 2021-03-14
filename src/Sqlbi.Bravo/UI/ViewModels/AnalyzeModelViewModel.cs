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
        private const int NumberOfRowsInSummary = 5;
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private readonly List<VpaTableColumnViewModel> _allTablesCache = new List<VpaTableColumnViewModel>();
        private List<VpaColumn> _unusedColumns;
        private bool _initialized = false;
        private bool _unreferencedColumnsOnly;

        public AnalyzeModelViewModel(IAnalyzeModelService service, ILogger<AnalyzeModelViewModel> logger)
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
                OnPropertyChanged(nameof(SummaryColumns));
            }
        }

        public List<VpaColumnViewModel> SummaryColumns
        {
            get
            {
                return UnreferencedColumnsOnly
                    ? UnusedColumns?.OrderByDescending(c => c.TotalSize)
                                    .Take(NumberOfRowsInSummary)
                                    .Select(c => new VpaColumnViewModel(this, c))
                                    .ToList()
                    : AllColumns.OrderByDescending(c => c.TotalSize)
                                .Take(NumberOfRowsInSummary)
                                .ToList();
            }
        }

        public bool UnreferencedColumnsOnly
        {
            get => _unreferencedColumnsOnly;
            set
            {
                _logger.Information(LogEvents.AnalyzeModelViewAction, "{@Details}", new object[] { new
                {
                    Action = "UnreferencedColumnsOnly",
                    Value = value
                }});

                SetProperty(ref _unreferencedColumnsOnly, value);
                OnPropertyChanged(nameof(SummaryColumns));
                OnPropertyChanged(nameof(SummaryColumnSize));
                OnPropertyChanged(nameof(SummaryColumnWeight));
                OnPropertyChanged(nameof(SummaryColumnWeightAngle));
                OnPropertyChanged(nameof(SummaryListedColumnPercentage));
                OnPropertyChanged(nameof(SummaryUnlistedColumnPercentage));
            }
        }

        public long? SummaryColumnSize => SummaryColumns?.Sum(c => c.TotalSize);

        public double? SummaryColumnWeight => SummaryColumns?.Sum(c => c.PercentageDatabase);

        public double? SummaryColumnWeightAngle => SummaryColumnWeight * 360;

        public double SummaryListedColumnPercentage => (SummaryColumnWeight ?? 0) * 100;

        public double SummaryUnlistedColumnPercentage => 100 - SummaryListedColumnPercentage;

        public IEnumerable<VpaTableColumnViewModel> AllTableColumns
        {
            get
            {
                if (!_allTablesCache.Any())
                {
                    VpaTableColumnViewModel currentTable = null;

                    var cols = _modelService.GetAllColumns()?
                                            .OrderByDescending(c => c.Table.ColumnsTotalSize)
                                            .ThenByDescending(c => c.TotalSize);

                    if (cols != null)
                    {
                        foreach (var col in cols)
                        {
                            if (currentTable == null || currentTable.TableName != col.Table.TableName)
                            {
                                currentTable = new VpaTableColumnViewModel(this, col)
                                {
                                    TotalSize = col.Table.ColumnsTotalSize,
                                    PercentageDatabase = col.Table.PercentageDatabase
                                };

                                // We're building the table structure from this row so have to be sure to include it too
                                currentTable.Columns.Add(new VpaTableColumnViewModel(this, col, currentTable));

                                _allTablesCache.Add(currentTable);
                            }
                            else
                            {
                                // Table doesn't expose this so have to sum it manually.
                                currentTable.Cardinality += col.ColumnCardinality;

                                currentTable.Columns.Add(new VpaTableColumnViewModel(this, col, currentTable));
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

        public IEnumerable<VpaColumnViewModel> AllColumns
            => AllTableColumns?.SelectMany(t => t.Columns).OrderByDescending(c => c.TotalSize);

        public IEnumerable<VpaTable> AllTables => _modelService.GetAllTables();

        public int? SelectedColumnCount => AllColumns?.Count(c => c.IsSelected ?? false) ?? 0;

        public long? SelectedColumnSize => AllColumns?.Where(c => c.IsSelected ?? false).Sum(c => c.TotalSize);

        public double? SelectedColumnWeight => AllColumns?.Where(c => c.IsSelected ?? false).Sum(c => c.PercentageDatabase);

        public string TimeSinceLastSync
                    => LastSyncTime.Year == 1
                    ? "not yet"
                    : $"{(DateTime.UtcNow - LastSyncTime).Humanize(minUnit: Humanizer.Localisation.TimeUnit.Second).Replace("minute", "min").Replace("second", "sec")} ago";

        private async Task RefreshAsync()
        {
            _logger.Information(LogEvents.AnalyzeModelViewAction, "{@Details}", new object[] { new
            {
                Action = "Refresh"
            }});

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
            catch (Exception ex)
            {
                _logger.Error(LogEvents.AnalyzeModelException, ex);

                ParentTab.DisplayError($"Unable to connect{Environment.NewLine}{ex.Message}", InitializeOrRefreshModelAnalyzer);
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
                _logger.Information(LogEvents.AnalyzeModelViewAction, "{@Details}", new object[] { new
                {
                    Action = "ExportVpax"
                }});
                
                VpaxTools.ExportVpax(saveFileDialog.FileName, _modelService.GetModelForExport());
            }

            await Task.CompletedTask;
        }

        private void ShowHelp()
        {
            _logger.Information(LogEvents.AnalyzeModelViewAction, "{@Details}", new object[] { new
            {
                Action = "ShowHelp"
            }});

            Views.ShellView.Instance.ShowMediaDialog(new HowToAnalyzeModelHelp());
        }

        private void ShowWarningHelp()
        {
            _logger.Information(LogEvents.AnalyzeModelViewAction, "{@Details}", new object[] { new
            {
                Action = "ShowWarningHelp"
            }});

            Views.ShellView.Instance.ShowMediaDialog(new ColumnOptimizationHelp());
        }

        private async Task ShowMoreDetailsAsync()
        {
            _logger.Information(LogEvents.AnalyzeModelViewAction, "{@Details}", new object[] { new
            {
                Action = "ShowMoreDetails"
            }});

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

        private readonly Dictionary<string, System.Windows.Media.Color> _colorCache = new Dictionary<string, System.Windows.Media.Color>();

        internal System.Windows.Media.Color GetTableColor(string tableName)
        {
            if (_colorCache.ContainsKey(tableName))
            {
                return _colorCache[tableName];
            }

            var orderedTables = AllTableColumns.OrderByDescending(t => t.TotalSize).ToList();

            System.Windows.Media.Color result;

            // TODO REQUIREMENTS: Need to define the actual colors to use.
            switch (orderedTables.FindIndex(t => t.TableName.Equals(tableName)))
            {
                case 0:
                    result = System.Windows.Media.Colors.Orange;
                    break;
                case 1:
                    result = System.Windows.Media.Colors.Yellow;
                    break;
                case 2:
                    result = System.Windows.Media.Colors.LightGreen;
                    break;
                case 3:
                    result = System.Windows.Media.Colors.Green;
                    break;
                case 4:
                    result = System.Windows.Media.Colors.LightBlue;
                    break;
                case 5:
                    result = System.Windows.Media.Colors.Blue;
                    break;
                case 6:
                    result = System.Windows.Media.Colors.DarkBlue;
                    break;
                case 7:
                    result = System.Windows.Media.Colors.Purple;
                    break;
                default:
                    result = System.Windows.Media.Colors.Red;  // Default
                    break;
            }

            _colorCache.Add(tableName, result);
            
            return result;
        }
    }
}
