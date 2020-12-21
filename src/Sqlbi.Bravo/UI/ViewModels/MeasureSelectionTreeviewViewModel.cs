using Sqlbi.Bravo.UI.Framework.ViewModels;
using System.Collections.ObjectModel;
using System.Linq;

namespace Sqlbi.Bravo.UI.ViewModels
{
    internal class MeasureSelectionViewModel : BaseViewModel
    {
        private string _searchText;

        public ObservableCollection<TreeItem> Tables { get; } = new ObservableCollection<TreeItem>();

        public TreeItem SelectedTreeViewItem { get; set; }

        public int SelectedTreeItemCount => Tables.Sum(t => t.Measures.Count(m => m.IsSelected ?? false));

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    foreach (var table in Tables)
                    {
                        table.RefreshFilter();
                    }
                }
            }
        }

        internal void RecalculateSelectedCount() => OnPropertyChanged(nameof(SelectedTreeItemCount));
    }
}
