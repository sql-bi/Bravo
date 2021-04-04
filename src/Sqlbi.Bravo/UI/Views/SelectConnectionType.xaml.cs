using Dax.Vpax.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Sqlbi.Bravo.Core.Logging;
using Sqlbi.Bravo.Core.Services.Interfaces;
using Sqlbi.Bravo.UI.DataModel;
using Sqlbi.Bravo.UI.ViewModels;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
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

            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void HowToUseClicked(object sender, RoutedEventArgs e) => ShellView.Instance.ShowMediaDialog(new HowToUseBravoHelp());

        private void AttachToWindowClicked(object sender, RoutedEventArgs e)
        {
            _logger.Trace();

            var instances = App.ServiceProvider.GetRequiredService<IPowerBIDesktopService>().GetInstances();

            foreach (var instance in instances)
            {
                _ = MessageBox.Show($"{ instance.Name } @ { instance.LocalEndPoint }", "TODO", MessageBoxButton.OK);                
            }

            // TODO REQUIREMENTS: need to know how to connect here
            _ = MessageBox.Show(
                "Need to attach to an active window",
                "TODO",
                MessageBoxButton.OK,
                MessageBoxImage.Question);

            _logger.Information(LogEvents.StartConnectionAction, "{@Details}", new object[] { new
            {
                Action = "AttachPowerBIDesktop"
            }});
        }

        private async void ConnectToDatasetClicked(object sender, RoutedEventArgs e)
        {
            _logger.Trace();

            #region Test

            var service = App.ServiceProvider.GetRequiredService<IPowerBICloudService>();
            if (service.IsAuthenticated == false)
            {
                var loggedIn = await service.LoginAsync();
                if (loggedIn == false)
                {
                    return;
                }
            }

            _ = MessageBox.Show($"{ service.Account.Username } - { service.Account.Environment } @ TenantId { service.Account.HomeAccountId.TenantId } ", "TODO", MessageBoxButton.OK);

            var datasets = await service.GetSharedDatasetsAsync();
            var workspaceCount = datasets.Select((d) => d.WorkspaceId).Distinct().Count();
            var modelCount = datasets.Select((d) => d.Model.Id).Distinct().Count();

            _ = MessageBox.Show($"{ workspaceCount } workspaces and { modelCount } models found", "TODO", MessageBoxButton.OK);

            //await service.LogoutAsync();

            #endregion

            // TODO REQUIREMENTS: need to know how to connect here
            _ = MessageBox.Show(
                "Need to sign-in and connect to a dataset",
                "TODO",
                MessageBoxButton.OK,
                MessageBoxImage.Question);

            _logger.Information(LogEvents.StartConnectionAction, "{@Details}", new object[] { new
            {
                Action = "ConnectPowerBIDataset"
            }});
        }

        private void OpenVertipaqFileClicked(object sender, RoutedEventArgs e)
        {
            _logger.Trace();

            var openFileDialog = new OpenFileDialog
            {
                CheckFileExists = true,
                Multiselect = false,
                Filter = "Vertipaq files (*.vpax)|*.vpax",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _logger.Information(LogEvents.StartConnectionAction, "{@Details}", new object[] { new
                {
                    Action = "OpenVertipaqFile"
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
