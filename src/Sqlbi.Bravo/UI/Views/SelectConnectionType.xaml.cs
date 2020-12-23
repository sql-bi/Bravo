using Microsoft.Win32;
using Sqlbi.Bravo.UI.DataModel;
using Sqlbi.Bravo.UI.ViewModels;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Sqlbi.Bravo.UI.Views
{
    public partial class SelectConnectionType : Page
    {
        public SelectConnectionType() => InitializeComponent();

        private void RequestNavigateHyperlink(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private async void HowToUseClicked(object sender, RoutedEventArgs e)
        {
            await ShellView.Instance.ShowMediaDialog(new HowToUseBravoHelp());
        }

        private void AttachToWindowClicked(object sender, RoutedEventArgs e)
        {
            // TODO REQUIREMENTS: need to know how to connect here
            _ = MessageBox.Show(
                "Need to attach to an active window",
                "TODO",
                MessageBoxButton.OK,
                MessageBoxImage.Question);

            /*
            var vm = DataContext as ShellViewModel;

            vm.SelectedTab.ConnectionName = "TOFIX: Title from ParentProcess";

            vm.SelectedTab.ConnectionType = BiConnectionType.ActivePowerBiWindow;
            vm.SelectedTab.ContentPageSource = vm.SelectedItem.NavigationPage;
            */
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
                // TODO REQUIREMENTS: need to load the file
                _ = MessageBox.Show(
                    $"Need to load the Vertipaq file: {openFileDialog.FileName}",
                    "TODO",
                    MessageBoxButton.OK,
                    MessageBoxImage.Question);
            }
        }
    }
}
