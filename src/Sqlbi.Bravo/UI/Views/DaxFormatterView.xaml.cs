using MahApps.Metro.Controls.Dialogs;
using System.Windows;
using System.Windows.Controls;

namespace Sqlbi.Bravo.UI.Views
{
    public partial class DaxFormatterView : UserControl
    {
        public DaxFormatterView() => InitializeComponent();

        private async void DoneClicked(object sender, RoutedEventArgs e)
        {
            var dlgSettings = new MetroDialogSettings {
                AffirmativeButtonText = "Refresh",
                NegativeButtonText = "Cancel"
                };

            var dlgResult = await ShellView.Instance.ShowMessageAsync(
                "Formatting complete",
                "Press Refresh to get any updated DAX formulas to be formatted.",
                MessageDialogStyle.AffirmativeAndNegative,
                dlgSettings);

            if (dlgResult == MessageDialogResult.Affirmative)
            {
                (DataContext as ViewModels.DaxFormatterViewModel).RefreshCommand.Execute(null);
            }
        }

        private void TreeviewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // DataContext may not be set here if itemChange event happens when closing a tab
            if (DataContext is not null)
            {
                // Quick hack for not being able to bind the selected item in WPF
                (DataContext as ViewModels.DaxFormatterViewModel).SelectionTreeData.SelectedTreeViewItem = (ViewModels.TreeItem)e?.NewValue;
            }
        }

        private void CopyToClipboardClicked(object sender, RoutedEventArgs e) =>
            // Doing this in code-behind for simplicity and preserving UI separation
            Clipboard.SetText((DataContext as ViewModels.DaxFormatterViewModel).NeedFormattingSelected.FormatterDax);
    }
}
