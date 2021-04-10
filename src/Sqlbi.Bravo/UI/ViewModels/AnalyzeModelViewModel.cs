using Dax.Metadata;
using Dax.ViewModel;
using Dax.Vpax.Tools;
using Humanizer;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Sqlbi.Bravo.Core;
using Sqlbi.Bravo.Core.Helpers;
using Sqlbi.Bravo.Core.Logging;
using Sqlbi.Bravo.Core.Services.Interfaces;
using Sqlbi.Bravo.UI.DataModel;
using Sqlbi.Bravo.UI.Framework.Commands;
using Sqlbi.Bravo.UI.Framework.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Sqlbi.Bravo.UI.ViewModels
{
    internal class AnalyzeModelViewModel : BaseViewModel
    {
        internal const int SubViewIndex_Loading = 0;
        internal const int SubViewIndex_Summary = 1;
        internal const int SubViewIndex_Details = 2;

        private readonly List<VpaTableColumnViewModel> _allTablesCache = new List<VpaTableColumnViewModel>();
        private readonly Dictionary<string, Color> _colorCache = new Dictionary<string, Color>();
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private readonly IAnalyzeModelService _analyzer;
        private readonly ILogger _logger;
        private List<VpaColumn> _unusedColumns;
        private bool _unreferencedColumnsOnly;
        private bool _initialized;

        public AnalyzeModelViewModel(IAnalyzeModelService analyzer, ILogger<AnalyzeModelViewModel> logger)
        {
            _analyzer = analyzer;
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

        public DateTime LastSyncTime => _analyzer.LastSyncTime;

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

        public string UnusedColumnsSize => UnusedColumns?.Sum((c) => c.TotalSize).Bytes().ToString("#.#") ?? "-";

        public int TableCount => AllTables?.Count() ?? 0;

        public int AllColumnCount => AllColumns?.Count() ?? 0;

        public int MeasuresCount => AllTables?.Sum((t) => t.Measures.Count()) ?? 0;

        public string TotalDbSize => AllTables?.Sum((t) => t.TableSize).Bytes().ToString("#.#") ?? "-";

        public long MaxRowsCount => AllTables?.Max((t) => t.RowsCount) ?? 0;

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
                List<VpaColumnViewModel> columns;

                if (UnreferencedColumnsOnly)
                {
                    columns = UnusedColumns?.OrderByDescending((c) => c.TotalSize)
                        .Take(AppConstants.AnalyzeModelSummaryColumnCount)
                        .Select((c) => new VpaColumnViewModel(this, c))
                        .ToList();
                }
                else
                {
                    columns = AllColumns.OrderByDescending((c) => c.TotalSize)
                        .Take(AppConstants.AnalyzeModelSummaryColumnCount)
                        .ToList();
                }

                return columns;
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

        public long? SummaryColumnSize => SummaryColumns?.Sum((c) => c.TotalSize);

        public double? SummaryColumnWeight => SummaryColumns?.Sum((c) => c.PercentageDatabase);

        public double? SummaryColumnWeightAngle => SummaryColumnWeight * 360;

        public double SummaryListedColumnPercentage => (SummaryColumnWeight ?? 0) * 100;

        public double SummaryUnlistedColumnPercentage => 100 - SummaryListedColumnPercentage;

        public IEnumerable<VpaTableColumnViewModel> AllTableColumns
        {
            get
            {
                if (_allTablesCache.Any() == false)
                {
                    VpaTableColumnViewModel currentTable = null;

                    var columns = _analyzer.AllColumns?
                        .OrderByDescending((c) => c.Table.ColumnsTotalSize)
                        .ThenByDescending((c) => c.TotalSize);

                    if (columns != null)
                    {
                        foreach (var column in columns)
                        {
                            if (currentTable == null || currentTable.TableName != column.Table.TableName)
                            {
                                currentTable = new VpaTableColumnViewModel(this, column)
                                {
                                    TotalSize = column.Table.ColumnsTotalSize,
                                    PercentageDatabase = column.Table.PercentageDatabase
                                };

                                // We're building the table structure from this row so have to be sure to include it too
                                currentTable.Columns.Add(new VpaTableColumnViewModel(this, column, currentTable));

                                _allTablesCache.Add(currentTable);
                            }
                            else
                            {
                                // Table doesn't expose this so have to sum it manually.
                                currentTable.Cardinality += column.ColumnCardinality;
                                currentTable.Columns.Add(new VpaTableColumnViewModel(this, column, currentTable));
                            }
                        }
                    }
                }

                return _allTablesCache;
            }
        }

        public IEnumerable<VpaColumnViewModel> AllColumns => AllTableColumns?.SelectMany((t) => t.Columns).OrderByDescending((c) => c.TotalSize);

        public IEnumerable<VpaTable> AllTables => _analyzer.AllTables;

        public int? SelectedColumnCount => AllColumns?.Count((c) => c.IsSelected ?? false) ?? 0;

        public long? SelectedColumnSize => AllColumns?.Where((c) => c.IsSelected ?? false).Sum((c) => c.TotalSize);

        public double? SelectedColumnWeight => AllColumns?.Where((c) => c.IsSelected ?? false).Sum((c) => c.PercentageDatabase);

        public string TimeSinceLastSync => LastSyncTime.HumanizeElapsed();

        internal void EnsureInitialized()
        {
            _logger.Trace();

            if (_initialized == false)
            {
                Task.Run(async () => await RefreshAsync());
            }
        }

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

                    await InitializeOrRefreshModelAnalyzer();
                }
                finally
                {
                    ViewIndex = lastIndex > 0 ? lastIndex : SubViewIndex_Summary;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(LogEvents.AnalyzeModelException, ex);

                ParentTab.DisplayError($"Unable to connect{ Environment.NewLine }{ ex.Message }", InitializeOrRefreshModelAnalyzer);
            }
        }

        private async Task ExportVpaxAsync()
        {
            _logger.Trace();

            var saveFileDialog = new SaveFileDialog
            {
                FileName = $"{ ParentTab.ConnectionName }.vpax",
                Filter = "VertiPaq Analyzer file (*.vpax)|*.vpax",
                DefaultExt = ".vpax",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                _logger.Information(LogEvents.AnalyzeModelViewAction, "{@Details}", new object[] { new
                {
                    Action = "ExportVpax"
                }});

                await _analyzer.ExportVertiPaqAnalyzerModel(path: saveFileDialog.FileName);
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
            _logger.Trace();

            ViewIndex = SubViewIndex_Summary;

            await Task.CompletedTask;
        }

        private async Task InitializeOrRefreshModelAnalyzer()
        {
            _logger.Trace();

            LoadingDetails = "Connecting to data";

            await _analyzer.InitilizeOrRefreshAsync(ParentTab.ConnectionSettings);

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
            _logger.Trace();

            _allTablesCache.Clear();

            var summary = _analyzer.DatasetSummary;
            DatasetSize = summary.DatasetSize.Bytes().ToString("#.#");
            DatasetColumnCount = summary.ColumnCount;
            LargestColumnSize = AllColumns?.Any() ?? false ? AllColumns?.Max((c) => c.TotalSize) ?? 0 : 0;

            // Sort these here so the DataGrid doesn't have to worry about loading all rows to be able to sort
            UnusedColumns = _analyzer.UnusedColumns.OrderByDescending((c) => c.PercentageDatabase).ToList();
        }

        internal Color GetTableColor(string tableName)
        {
            _logger.Trace();

            if (_colorCache.ContainsKey(tableName))
            {
                return _colorCache[tableName];
            }

            var sortedColumns = AllTableColumns.OrderByDescending((t) => t.TotalSize).ToList();
            var sizeIndex = sortedColumns.FindIndex((c) => c.TableName.Equals(tableName));

            var color = sizeIndex switch
            {
                0 => Colors.Orange,
                1 => Colors.Yellow,
                2 => Colors.LightGreen,
                3 => Colors.Green,
                4 => Colors.LightBlue,
                5 => Colors.Blue,
                6 => Colors.DarkBlue,
                7 => Colors.Purple,
                _ => Colors.Red, // Default
            };

            _colorCache.Add(tableName, color);
            
            return color;
        }
    }
}