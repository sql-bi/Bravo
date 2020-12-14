using Sqlbi.Bravo.UI.DataModel;
using Sqlbi.Bravo.UI.ViewModels;
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
            var vm = DataContext as ShellViewModel;

            // TODO: Get title from parent process
            vm.SelectedTab.ConnectionName = "TOFIX: Title from ParentProcess";

            vm.SelectedTab.ConnectionType = BiConnectionType.ActivePowerBiWindow;
            vm.SelectedTab.ContentPageSource = vm.SelectedItem.NavigationPage;
        }

        private void UseDummyDataClicked(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as ShellViewModel;

            vm.SelectedTab.ConnectionName = "DEMO DATA";
            vm.SelectedTab.ConnectionType = BiConnectionType.DemoMode;
            vm.SelectedTab.ContentPageSource = vm.SelectedItem.NavigationPage;
        }
    }
}
