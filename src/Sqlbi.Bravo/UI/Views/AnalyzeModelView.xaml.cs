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

        private void TablesTreeview_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (e.OriginalSource is TreeView tv)
            {
                // There should always be items but sometimes they don't get displayed
                if (!tv.HasItems)
                {
                    // Force the control to refresh
                    // So they will be displayed
                    tv.Items.Refresh();
                    tv.UpdateLayout();
                }
            }
        }
    }
}
