using Humanizer;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Core.Client.Http;
using Sqlbi.Bravo.Core.Helpers;
using Sqlbi.Bravo.Core.Logging;
using Sqlbi.Bravo.Core.Services;
using Sqlbi.Bravo.Core.Services.Interfaces;
using Sqlbi.Bravo.UI.DataModel;
using Sqlbi.Bravo.UI.Framework.Commands;
using Sqlbi.Bravo.UI.Framework.Helpers;
using Sqlbi.Bravo.UI.Framework.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace Sqlbi.Bravo.UI.ViewModels
{
    internal class DaxFormatterViewModel : BaseViewModel
    {
        private readonly IDaxFormatterService _formatter;
        private readonly IAnalysisServicesEventWatcherService _watcher;
        private readonly ILogger _logger;
        internal const int SubViewIndex_Loading = 0;
        internal const int SubViewIndex_Start = 1;
        internal const int SubViewIndex_ChooseFormulas = 2;
        internal const int SubViewIndex_Progress = 3;
        internal const int SubViewIndex_Changes = 4;
        internal const int SubViewIndex_Finished = 5;
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private bool _initialized = false;

        public DaxFormatterViewModel(IDaxFormatterService formatter, IAnalysisServicesEventWatcherService watcher, ILogger<DaxFormatterViewModel> logger)
        {
            _formatter = formatter;
            _watcher = watcher;
            _logger = logger;

            _logger.Trace();
            _watcher.OnEvent += OnAnalysisServicesEvent;
            //_watcher.OnConnectionStateChanged += OnAnalysisServicesConnectionStateChanged;

            ViewIndex = SubViewIndex_Loading;
            PreviewChanges = true;

            InitializeCommand = new RelayCommand(execute: async () => await InitializeAsync());
            FormatAnalyzeCommand = new RelayCommand(execute: async () => await AnalyzeAsync());
            FormatMakeChangesCommand = new RelayCommand(execute: async () => await MakeChangesAsync(), canExecute: () => !InitializeCommandIsEnabled && TabularObjectType != DaxFormatterTabularObjectType.None).ObserveProperties(this, nameof(TabularObjectType), nameof(InitializeCommandIsEnabled));
            HelpCommand = new RelayCommand(execute: async () => await ShowHelpAsync());
            RefreshCommand = new RelayCommand(execute: async () => await RefreshAsync());
            ChangeFormulasCommand = new RelayCommand(() => ChooseFormulas());
            ApplySelectedFormulaChangesCommand = new RelayCommand(() => SelectedFormulasChanged());
            OpenLogCommand = new RelayCommand(() => OpenLog());

            _timer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 15)
            };
            _timer.Tick += new EventHandler((s, e) => OnPropertyChanged(nameof(TimeSinceLastSync)));
            _timer.Start();
        }

        private DaxFormatterTabularObjectType TabularObjectType { get; set; } = DaxFormatterTabularObjectType.None;

        public TabItemViewModel ParentTab { get; set; }

        public int ViewIndex { get; set; }

        public bool PreviewChanges { get; set; }

        public ICommand OpenLogCommand { get; set; }

        public ICommand HelpCommand { get; set; }

        public ICommand RefreshCommand { get; set; }

        public ICommand InitializeCommand { get; set; }

        public ICommand ChangeFormulasCommand { get; set; }

        public ICommand ApplySelectedFormulaChangesCommand { get; set; }

        public ICommand SelectedTableMeasureChangedCommand { get; set; }

        public bool InitializeCommandIsRunning { get; set; }

        public bool InitializeCommandIsEnabled { get; set; } = true;

        public ICommand FormatAnalyzeCommand { get; set; }

        public ICommand FormatMakeChangesCommand { get; set; }

        public bool FormatCommandIsRunning { get; set; }

        public bool FormatCommandIsEnabled { get; set; }

        public MeasureSelectionViewModel SelectionTreeData { get; set; }

        public string ProgressDetails { get; set; }

        public MeasureInfoViewModel AllFormulasSelected { get; set; }

        public MeasureInfoViewModel NeedFormattingSelected { get; set; }

        public bool TabularObjectMeasuresIsChecked
        {
            get => TabularObjectType.HasFlag(DaxFormatterTabularObjectType.Measures);
            set => TabularObjectType = TabularObjectType.WithFlag(DaxFormatterTabularObjectType.Measures, set: value);
        }

        public string LoadingDetails { get; set; }

        public DateTime LastSyncTime { get; private set; }

        public string TimeSinceLastSync
            => LastSyncTime.Year == 1
            ? "not yet"
            : $"{(DateTime.UtcNow - LastSyncTime).Humanize(minUnit: Humanizer.Localisation.TimeUnit.Second).Replace("minute", "min").Replace("second", "sec")} ago";

        public int TabularObjectMeasuresCount { get; set; }

        public bool TabularObjectCalculatedColumnsIsChecked
        {
            get => TabularObjectType.HasFlag(DaxFormatterTabularObjectType.CalculatedColumns);
            set => TabularObjectType = TabularObjectType.WithFlag(DaxFormatterTabularObjectType.CalculatedColumns, set: value);
        }

        public int TabularObjectCalculatedColumnsCount { get; set; }

        public int MeasuresFormatted { get; set; }

        public int AnalyzedMeasureCount { get; set; }

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

        public ObservableCollection<MeasureInfoViewModel> Measures { get; set; } = new ObservableCollection<MeasureInfoViewModel>();

        public ObservableCollection<MeasureInfoViewModel> MeasuresNeedingFormatting
            => new ObservableCollection<MeasureInfoViewModel>(Measures.Where(m => !m.IsAlreadyFormatted).ToList());

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
            LoadingDetails = "Connecting to data";
            await _formatter.InitilizeOrRefreshAsync(ParentTab.RuntimeSummary);

            LastSyncTime = DateTime.UtcNow;
            OnPropertyChanged(nameof(TimeSinceLastSync));

            TabularObjectMeasuresCount = _formatter.Count(DaxFormatterTabularObjectType.Measures);
            TabularObjectCalculatedColumnsCount = _formatter.Count(DaxFormatterTabularObjectType.CalculatedColumns);
            TabularObjectKPIsCount = _formatter.Count(DaxFormatterTabularObjectType.KPIs);
            TabularObjectDetailRowsDefinitionsCount = _formatter.Count(DaxFormatterTabularObjectType.DetailRowsDefinitions);
            TabularObjectCalculationItemsCount = _formatter.Count(DaxFormatterTabularObjectType.CalculationItems);

            FormatCommandIsEnabled = true;
            InitializeCommandIsEnabled = false;

            LoadMeasuresForSelection();

            _initialized = true;
        }

        private void LoadMeasuresForSelection()
        {
            var measureList = _formatter.GetMeasures();

            var msvm = new MeasureSelectionViewModel();

            foreach (var measure in measureList)
            {
                var addedMeasure = false;

                foreach (var table in msvm.Tables)
                {
                    if (table.Name == measure.Table.Name)
                    {
                        table.Measures.Add(new TreeItem(msvm, table) { Name = measure.Name, Formula = measure.Expression });
                        addedMeasure = true;
                        break;
                    }
                }

                if (!addedMeasure)
                {
                    var newTable = new TreeItem(msvm) { Name = measure.Table.Name };
                    newTable.Measures.Add(new TreeItem(msvm, newTable) { Name = measure.Name, Formula = measure.Expression });
                    msvm.Tables.Add(newTable);
                }
            }

            SelectionTreeData = msvm;
        }

        internal void EnsureInitialized()
        {
            if (!_initialized)
            {
                // Hacky way to call async method because can't make this async as called from a setter
                Task.Run(() => RefreshAsync()).GetAwaiter().GetResult();
            }
        }

        private async Task RefreshAsync()
        {
            _logger.Trace();

            try
            {
                await ExecuteCommandAsync(() => InitializeCommandIsRunning,
                    async () =>
                    {
                        await InitializeOrRefreshFormatter();
                        ViewIndex = SubViewIndex_Start;
                    });
            }
            catch (Exception exc)
            {
                ParentTab.DisplayError($"Unable to connect{Environment.NewLine}{exc.Message}", InitializeOrRefreshFormatter);
            }
        }

        private void ChooseFormulas()
        {
            ViewIndex = SubViewIndex_ChooseFormulas;
        }

        private void SelectedFormulasChanged()
        {
            ViewIndex = SubViewIndex_Start;
        }

        private void OpenLog()
        {
            // TODO REQUIREMENTS: Open log file
        }

        private async Task ShowHelpAsync()
        {
            await Views.ShellView.Instance.ShowMediaDialog(new HowToFormatCodeHelp());
        }

        private async Task InitializeAsync()
        {
            _logger.Trace();

            await ExecuteCommandAsync(() => InitializeCommandIsRunning, InitializeOrRefreshFormatter);
        }

        private async Task AnalyzeAsync()
        {
            ProgressDetails = "Identifying formulas to format";
            ViewIndex = SubViewIndex_Progress;

            TabularObjectType = TabularObjectType.WithFlag(DaxFormatterTabularObjectType.Measures, true);

            //var measuresOfInterest = SelectionTreeData.Tables.SelectMany(t => t.Measures.Where(m => !string.IsNullOrWhiteSpace(m.Formula) && (m.IsSelected ?? false)));

            var runtimeSummary = ParentTab.RuntimeSummary;

            // TODO REQUIREMENTS: This should not get all Measures--only the ones that are in the `measuresOfInterest` (above)
            var measures = await _formatter.GetFormattedItems(TabularObjectType, runtimeSummary);

            Measures.Clear();

            string GetMeasureName(string id, string unformattedExpression)
            {
                foreach (var table in SelectionTreeData.Tables)
                {
                    var measureInfo = table.Measures.FirstOrDefault(t => t.Formula?.Trim() == unformattedExpression.Trim());

                    if (measureInfo != null)
                    {
                        return measureInfo.Name;
                    }
                }

                return id;
            }

            foreach (var m in measures)
            {
                Measures.Add(new MeasureInfoViewModel
                {
                    Identifier = m.Key,
                    // TODO REQUIREMENTS: Get names of measures at the same time as IDs and expressions - this is a workaround
                    Name = GetMeasureName(m.Key, m.Value.Item1),
                    OriginalDax = m.Value.Item1,
                    FormatterDax = m.Value.Item2,
                });
            }

            OnPropertyChanged(nameof(MeasuresNeedingFormatting));

            AnalyzedMeasureCount = Measures.Count;

            if (PreviewChanges)
            {
                NeedFormattingSelected =
                AllFormulasSelected = Measures.First();
                ViewIndex = SubViewIndex_Changes;
            }
            else
            {
                ProgressDetails = "Applying formatting changes";
                await ApplyFormattingChangesToModelAsync();

                ViewIndex = SubViewIndex_Finished;
            }
        }

        private async Task ApplyFormattingChangesToModelAsync()
        {
            await Task.Run(() =>
            {
                var toUpdate = new List<(string id, string expression)>();

                foreach (var measure in Measures)
                {
                    if (!measure.IsAlreadyFormatted && measure.Reformat)
                    {
                        toUpdate.Add((measure.Identifier, measure.FormatterDax));
                    }
                }

                // TODO: add error handling for this
                _formatter.SaveFormattedMeasures(toUpdate, ParentTab.RuntimeSummary);

                MeasuresFormatted = toUpdate.Count;
            });
        }

        private async Task MakeChangesAsync()
        {
            _logger.Trace();

            await ApplyFormattingChangesToModelAsync();

            ViewIndex = SubViewIndex_Finished;
        }
    }
}
