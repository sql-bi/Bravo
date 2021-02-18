using System.Windows.Controls;

namespace Sqlbi.Bravo.UI.Views
{
    public partial class AnalyzeModelView : UserControl
    {
        public AnalyzeModelView()
        {
            InitializeComponent();

            // This control exists as a single instance that is reused in different tabs.
            // When switching usage the DataContext is changed.
            DataContextChanged += AnalyzeModelView_DataContextChanged;
        }

        // If the DataContext has changed we may need to update the TreeMap (as it's not bound to the DataContext)
        private void AnalyzeModelView_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
            => TryRedrawTreeMap();

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
            => TryRedrawTreeMap();

        internal void TryRedrawTreeMap()
        {
            if (AnalyzeModelFlipView.SelectedIndex == 2)
            {
                var vm = DataContext as ViewModels.AnalyzeModelViewModel;

                if (vm.AllColumnCount > 0)
                {
                    TreeMap.DrawTree(vm.AllColumns);
                }
            }
        }
    }
}
