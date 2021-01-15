using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Core.Logging;
using Sqlbi.Bravo.UI.DataModel;
using Sqlbi.Bravo.UI.Framework.Commands;
using Sqlbi.Bravo.UI.Framework.ViewModels;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Sqlbi.Bravo.UI.ViewModels
{
    internal class AnalyzeModelViewModel : BaseViewModel
    {
        private readonly ILogger _logger;
        internal const int SubViewIndex_Loading = 0;
        internal const int SubViewIndex_Summary = 1;
        internal const int SubViewIndex_Details = 2;
        private readonly DispatcherTimer _timer = new DispatcherTimer();

        public AnalyzeModelViewModel(ILogger<DaxFormatterViewModel> logger)
        {
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

        public DateTime LastSyncTime { get; private set; }

        public bool LoadOrRefreshCommandIsRunning { get; set; }

        public ICommand LoadedCommand { get; set; }

        public ICommand HelpCommand { get; set; }

        public ICommand ExportVpaxCommand { get; set; }

        public ICommand RefreshCommand { get; set; }

        public ICommand MoreDetailsCommand { get; set; }

        public ICommand BackCommand { get; set; }

        public string LoadingDetails { get; set; }

        public int ViewIndex { get; set; }

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

        // TODO: REQUIREMENTS: NEED AN ACTUAL IMPLEMENTATION HERE
        private async Task InitializeOrRefreshModelAnalyzer()
        {
            LoadingDetails = "Connecting to data";
            await Task.Delay(1000);
            LoadingDetails = "retrieving data";
            await Task.Delay(1000);
            LoadingDetails = "Analyzing model";
            await Task.Delay(2000);

            ViewIndex = SubViewIndex_Summary;
        }
    }
}
