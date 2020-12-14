using Sqlbi.Bravo.UI.Framework.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Sqlbi.Bravo.UI.ViewModels
{
    internal class MeasureSelectionViewModel : BaseViewModel
    {
        public ObservableCollection<TreeItem> Tables { get; } = new ObservableCollection<TreeItem>();

        public TreeItem SelectedTreeViewItem { get; set; }

        public int SelectedTreeItemCount => Tables.Sum(t => t.Measures.Count(m => m.IsSelected ?? false));

        internal void RecalculateSelectedCount() => OnPropertyChanged(nameof(SelectedTreeItemCount));
    }

    internal class TreeItem : BaseViewModel
    {
        private readonly MeasureSelectionViewModel _parent;
        private bool? _isSelected = true;

        public TreeItem(MeasureSelectionViewModel parent) => _parent = parent;

        public bool? IsSelected
        {
            get => _isSelected;

            set
            {
                if (SetProperty(ref _isSelected, value))
                {
                    _parent.RecalculateSelectedCount();
                }
            }
        }

        public string Name { get; set; }

        public string Formula { get; set; }

        public ObservableCollection<TreeItem> Measures { get; } = new ObservableCollection<TreeItem>();
    }
}
