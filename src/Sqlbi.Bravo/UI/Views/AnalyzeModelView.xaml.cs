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
    }
}
