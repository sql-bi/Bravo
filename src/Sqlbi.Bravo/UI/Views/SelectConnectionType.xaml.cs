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

        private async void AttachToPowerBIDesktopClicked(object sender, RoutedEventArgs e)
        {
            // TODO REQUIREMENTS: need to know how to connect here
            _ = MessageBox.Show("Testing connection to Power BI Desktop", "TODO", MessageBoxButton.OK, MessageBoxImage.Question);

            var service = App.ServiceProvider.GetRequiredService<IPowerBIDesktopService>();
            var instances = await service.GetInstancesAsync();

            foreach (var instance in instances)
            {
                switch (MessageBox.Show($"Connect to instance '{ instance.Name }' @ '{ instance.LocalEndPoint }' ?", "TODO", MessageBoxButton.YesNoCancel))
                {
                    case MessageBoxResult.No:
                        continue;
                    case MessageBoxResult.Yes:
                        {
                            _logger.Information(LogEvents.StartConnectionAction, "{@Details}", new object[] { new
                            {
                                Action = "AttachPowerBIDesktop"
                            }});

                            var shellViewModel = App.ServiceProvider.GetRequiredService<ShellViewModel>();
                            await shellViewModel.AddNewTabAsync(instance);
                        }
                        return;
                    default:
                        return;
                }
            }
        }

        private async void ConnectToPowerBIDatasetClicked(object sender, RoutedEventArgs e)
        {
            _logger.Trace();

            // TODO REQUIREMENTS: need to know how to connect here
            _ = MessageBox.Show("Testing connection to Power BI dataset", "TODO", MessageBoxButton.OK, MessageBoxImage.Question);

            var service = App.ServiceProvider.GetRequiredService<IPowerBICloudService>();
            if (service.IsAuthenticated == false)
            {
                var loggedIn = await service.LoginAsync();
                if (loggedIn == false)
                {
                    return;
                }

                _ = MessageBox.Show($"Hello { service.Account.Username } @ TenantId { service.Account.HomeAccountId.TenantId }", "TODO", MessageBoxButton.OK);
            }

            var datasets = await service.GetDatasetsAsync();

            foreach (var dataset in datasets)
            {
                switch (MessageBox.Show($"Connect to workspace '{ dataset.WorkspaceName }' model '{ dataset.Model.DisplayName }' ?", "TODO", MessageBoxButton.YesNoCancel))
                {
                    case MessageBoxResult.No:
                        continue;
                    case MessageBoxResult.Yes:
                        {
                            _logger.Information(LogEvents.StartConnectionAction, "{@Details}", new object[] { new
                            {
                                Action = "ConnectPowerBIDataset"
                            }});

                            var shellViewModel = App.ServiceProvider.GetRequiredService<ShellViewModel>();
                            await shellViewModel.AddNewTabAsync(dataset);
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

            if (openFileDialog.ShowDialog() == false)
            {
                return;
            }

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