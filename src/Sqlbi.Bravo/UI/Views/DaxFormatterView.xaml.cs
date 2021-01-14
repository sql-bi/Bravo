using System.Windows;
using System.Windows.Controls;

namespace Sqlbi.Bravo.UI.Views
{
    public partial class DaxFormatterView : UserControl
    {
        public DaxFormatterView() => InitializeComponent();

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

        private void TreeviewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) =>
            // Quick hack for not being able to bind the selected item in WPF
            (DataContext as ViewModels.DaxFormatterViewModel).SelectionTreeData.SelectedTreeViewItem = (ViewModels.TreeItem)e.NewValue;

        private void CopyToClipboardClicked(object sender, RoutedEventArgs e) =>
            // Doing this in code-behind for simplicity and preserving UI separation
            Clipboard.SetText((DataContext as ViewModels.DaxFormatterViewModel).NeedFormattingSelected.FormatterDax);
    }
}
