using System.Windows.Controls;

namespace Sqlbi.Bravo.UI.Views
{
    public partial class AnalyzeModelView : UserControl
    {
        public AnalyzeModelView() => InitializeComponent();

        private void DatagridEditingHandler(object sender, DataGridBeginningEditEventArgs e)
        {
            //// The DataGrids cannot be edited but the default behavior allows it.
            //// If don't do this we get a crash 'EditItem is not allowed' when a text entry is doubleclicked.
            e.Cancel = true;
        }

        // Use this to detect when to redraw the TreeMap
        // The data must have been loaded to get to the FlipViewItem that contains it
        // When refreshing, the loading item is displayed, before going back to the item containing the TreeMap
        private void FlipViewSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is MahApps.Metro.Controls.FlipView flipView)
            {
                if (flipView.SelectedIndex == 2)
                {
                    TreeMap.DrawTree((DataContext as ViewModels.AnalyzeModelViewModel).AllColumns);
                }
            }
        }
    }
}
