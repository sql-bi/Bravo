using System;
using System.Windows;
using System.Windows.Controls;

namespace Sqlbi.Bravo.UI.Views
{
    public partial class DaxFormatterView : Page
    {
        public DaxFormatterView() => InitializeComponent();

        private void PretendProgressClicked(object sender, RoutedEventArgs e)
        {
            var vm = (DataContext as ViewModels.DaxFormatterViewModel);

            if (vm.PreviewChanges)
            {
                vm.ViewIndex =
                    ViewModels.DaxFormatterViewModel.SubViewIndex_Changes;
            }
            else
            {
                // TODO: Need to make the actual changes
                vm.ViewIndex =
                    ViewModels.DaxFormatterViewModel.SubViewIndex_Finished;
            }
        }

        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            _ = MessageBox.Show(
                "Need to know what to do here",
                "TODO",
                MessageBoxButton.OK,
                MessageBoxImage.Question);
        }

        private void DoneClicked(object sender, RoutedEventArgs e)
        {
            _ = MessageBox.Show(
                "Need to know what to do here",
                "TODO",
                MessageBoxButton.OK,
                MessageBoxImage.Question);
        }

        private void PretendErrorClick(object sender, RoutedEventArgs e)
        {
            (ShellView.Instance.DataContext
                as ViewModels.ShellViewModel).SelectedTab.DisplayError("Error code: blah blah blah", null);
        }
    }
}
