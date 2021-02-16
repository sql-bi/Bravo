using Dax.Vpax.Tools;
using Microsoft.Win32;
using Sqlbi.Bravo.UI.DataModel;
using Sqlbi.Bravo.UI.ViewModels;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Sqlbi.Bravo.UI.Views
{
    public partial class SelectConnectionType : UserControl
    {
        public SelectConnectionType() => InitializeComponent();

        private void RequestNavigateHyperlink(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void HowToUseClicked(object sender, RoutedEventArgs e)
            => ShellView.Instance.ShowMediaDialog(new HowToUseBravoHelp());

        private void AttachToWindowClicked(object sender, RoutedEventArgs e)
        {
            // TODO REQUIREMENTS: need to know how to connect here
            _ = MessageBox.Show(
                "Need to attach to an active window",
                "TODO",
                MessageBoxButton.OK,
                MessageBoxImage.Question);
        }

        private void ConnectToDatasetClicked(object sender, RoutedEventArgs e)
        {
            // TODO REQUIREMENTS: need to know how to connect here
            _ = MessageBox.Show(
                "Need to sign-in and connect to a dataset",
                "TODO",
                MessageBoxButton.OK,
                MessageBoxImage.Question);
        }

        private void OpenVertipaqFileClicked(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog {
                CheckFileExists = true,
                Multiselect = false,
                Filter = "Vertipaq files (*.vpax)|*.vpax",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var fileContent = VpaxTools.ImportVpax(openFileDialog.FileName);

                var vm = DataContext as TabItemViewModel;
                vm.ConnectionType = BiConnectionType.VertipaqAnalyzerFile;
                vm.ConnectionName = System.IO.Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                vm.AnalyzeModelVm.OnPropertyChanged(nameof(AnalyzeModelViewModel.ConnectionName));
                vm.ShowAnalysisOfLoadedModel(fileContent.DaxModel);
            }
        }
    }
}
