using MahApps.Metro.Controls.Dialogs;
using Sqlbi.Bravo.UI.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Sqlbi.Bravo.UI.Views
{
    public partial class DaxFormatterView : UserControl
    {
        public DaxFormatterView() => InitializeComponent();

        private DaxFormatterViewModel ViewModel => DataContext as DaxFormatterViewModel;

        private async void DoneClicked(object sender, RoutedEventArgs e)
        {
            var dialogSettings = new MetroDialogSettings 
            {
                AffirmativeButtonText = "Refresh",
                NegativeButtonText = "Cancel"
            };

            var dialogResult = await ShellView.Instance.ShowMessageAsync("Formatting complete", "Press Refresh to get any updated DAX formulas to be formatted.", MessageDialogStyle.AffirmativeAndNegative, dialogSettings);
            
            if (dialogResult == MessageDialogResult.Affirmative)
            {
                ViewModel.RefreshCommand.Execute(null);
            }
        }

        private void TreeviewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // DataContext or SelectionTreeData may not be sets here if itemChange event happens when closing a tab
            if (ViewModel?.SelectionTreeData is not null)
            {
                // Quick hack for not being able to bind the selected item in WPF
                ViewModel.SelectionTreeData.SelectedTreeViewItem = (TreeItem)e?.NewValue;
            }
        }

        private void CopyToClipboardClicked(object sender, RoutedEventArgs e)
        {
            // Doing this in code-behind for simplicity and preserving UI separation
            Clipboard.SetText(ViewModel.NeedFormattingSelected.FormatterDax);
        }
    }
}
