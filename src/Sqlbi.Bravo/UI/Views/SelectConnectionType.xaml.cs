using Dax.Vpax.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Sqlbi.Bravo.Core.Logging;
using Sqlbi.Bravo.UI.DataModel;
using Sqlbi.Bravo.UI.ViewModels;
using System;
using System.Diagnostics;
using System.IO;
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
                e.Uri.AbsoluteUri
            }});

            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void HowToUseClicked(object sender, RoutedEventArgs e) => ShellView.Instance.ShowMediaDialog(new HowToUseBravoHelp());

        private void AttachToWindowClicked(object sender, RoutedEventArgs e)
        {
            _logger.Trace();

            // TODO REQUIREMENTS: need to know how to connect here
            _ = MessageBox.Show(
                "Need to attach to an active window",
                "TODO",
                MessageBoxButton.OK,
                MessageBoxImage.Question);

            _logger.Information(LogEvents.ConnectionTypeAttachPowerBI);
        }

        private void ConnectToDatasetClicked(object sender, RoutedEventArgs e)
        {
            _logger.Trace();

            // TODO REQUIREMENTS: need to know how to connect here
            _ = MessageBox.Show(
                "Need to sign-in and connect to a dataset",
                "TODO",
                MessageBoxButton.OK,
                MessageBoxImage.Question);

            _logger.Information(LogEvents.ConnectionTypePowerBIDataset);
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
                _logger.Information(LogEvents.ConnectionTypeVertipaqFile);

                var fileContent = VpaxTools.ImportVpax(openFileDialog.FileName);

                var vm = DataContext as TabItemViewModel;
                vm.ConnectionType = BiConnectionType.VertipaqAnalyzerFile;
                vm.ConnectionName = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                vm.AnalyzeModelVm.OnPropertyChanged(nameof(AnalyzeModelViewModel.ConnectionName));
                vm.ShowAnalysisOfLoadedModel(fileContent.DaxModel);
            }
        }
    }
}
