using Dax.Vpax.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Sqlbi.Bravo.Core.Logging;
using Sqlbi.Bravo.Core.Services.Interfaces;
using Sqlbi.Bravo.Core.Settings;
using Sqlbi.Bravo.UI.DataModel;
using Sqlbi.Bravo.UI.ViewModels;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Sqlbi.Bravo.UI.Views
{
    public partial class SelectConnectionType : UserControl
    {
        private readonly ILogger _logger;

        public SelectConnectionType()
        {
            InitializeComponent();
            
            _logger = App.ServiceProvider.GetRequiredService<ILogger<SelectConnectionType>>();
        }

        private void RequestNavigateHyperlink(object sender, RequestNavigateEventArgs e)
        {
            _logger.Information(LogEvents.NavigateHyperlink, "{@Details}", new object[] { new
            {
                Uri = e.Uri.AbsoluteUri
            }});

            var startInfo = new ProcessStartInfo(e.Uri.AbsoluteUri) 
            { 
                UseShellExecute = true 
            };

            Process.Start(startInfo);

            e.Handled = true;
        }

        private void HowToUseClicked(object sender, RoutedEventArgs e) => ShellView.Instance.ShowMediaDialog(new HowToUseBravoHelp());

        private void AttachToPowerBIDesktopClicked(object sender, RoutedEventArgs e)
        {
            _logger.Trace();

            // TODO REQUIREMENTS: need to know how to connect here
            _ = MessageBox.Show(
                "Testing connection to the first Power BI Desktop instance available",
                "TODO",
                MessageBoxButton.OK,
                MessageBoxImage.Question);

            _logger.Information(LogEvents.StartConnectionAction, "{@Details}", new object[] { new
            {
                Action = "AttachPowerBIDesktop"
            }});

            var service = App.ServiceProvider.GetRequiredService<IPowerBIDesktopService>();
            var instances = service.GetInstances();

            _ = MessageBox.Show($"{ instances.Count() } active Power BI Desktop instances found", "TODO", MessageBoxButton.OK);

            var instance = instances.FirstOrDefault();
            if (instance == null)
            {
                return;
            }

            var shellViewModel = App.ServiceProvider.GetRequiredService<ShellViewModel>();
            var connectionSettings = ConnectionSettings.CreateFrom(instance);

            shellViewModel.AddNewTab(BiConnectionType.ActivePowerBiWindow, SubPage.AnalyzeModel, connectionSettings);
        }

        private async void ConnectToPowerBIDatasetClicked(object sender, RoutedEventArgs e)
        {
            _logger.Trace();

            // TODO REQUIREMENTS: need to know how to connect here
            _ = MessageBox.Show(
                "Testing connection to the first Power BI dataset available",
                "TODO",
                MessageBoxButton.OK,
                MessageBoxImage.Question);

            _logger.Information(LogEvents.StartConnectionAction, "{@Details}", new object[] { new
            {
                Action = "ConnectPowerBIDataset"
            }});

            var service = App.ServiceProvider.GetRequiredService<IPowerBICloudService>();
            if (service.IsAuthenticated == false)
            {
                var loggedIn = await service.LoginAsync();
                if (loggedIn == false)
                {
                    return;
                }
            }

            _ = MessageBox.Show($"Hello { service.Account.Username } @ TenantId { service.Account.HomeAccountId.TenantId }", "TODO", MessageBoxButton.OK);

            var datasets = await service.GetSharedDatasetsAsync();

            // TOFIX: add support to PersonalGroup and non-premium workspaces 
            //datasets = datasets.Where((d) => d.WorkspaceType != Client.PowerBI.PowerBICloud.Models.MetadataWorkspaceType.PersonalGroup);

            foreach (var dataset in datasets)
            {
                switch (MessageBox.Show($"Connect to workspace '{ dataset.WorkspaceName }' model '{ dataset.Model.DisplayName }' ?", "TODO", MessageBoxButton.YesNoCancel))
                {
                    case MessageBoxResult.No:
                        continue;
                    case MessageBoxResult.Yes:
                        {
                            var shellViewModel = App.ServiceProvider.GetRequiredService<ShellViewModel>();
                            var connectionSettings = ConnectionSettings.CreateFrom(dataset, service);

                            shellViewModel.AddNewTab(BiConnectionType.ActivePowerBiWindow, SubPage.AnalyzeModel, connectionSettings);
                        }
                        return;
                    default:
                        return;
                }
            }

            //await service.LogoutAsync();
        }

        private void OpenVertiPaqAnalyzerFileClicked(object sender, RoutedEventArgs e)
        {
            _logger.Trace();

            var openFileDialog = new OpenFileDialog
            {
                CheckFileExists = true,
                Multiselect = false,
                Filter = "VertiPaq Analyzer files (*.vpax)|*.vpax",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _logger.Information(LogEvents.StartConnectionAction, "{@Details}", new object[] { new
                {
                    Action = "OpenVertiPaqAnalyzerFile"
                }});

                var fileContent = VpaxTools.ImportVpax(openFileDialog.FileName);

                var viewModel = DataContext as TabItemViewModel;
                viewModel.ConnectionType = BiConnectionType.VertipaqAnalyzerFile;
                viewModel.ConnectionName = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                viewModel.AnalyzeModelVm.OnPropertyChanged(nameof(AnalyzeModelViewModel.ConnectionName));
                viewModel.ShowAnalysisOfLoadedModel(fileContent.DaxModel);
            }
        }
    }
}