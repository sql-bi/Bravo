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
        private readonly TreeItem _table;
        private bool? _isSelected = true;

        public TreeItem(MeasureSelectionViewModel parent) => _parent = parent;

        public TreeItem(MeasureSelectionViewModel parent, TreeItem table) : this(parent) => _table = table;

        public bool? IsSelected
        {
            get => _isSelected;

            set
            {
                if (SetProperty(ref _isSelected, value))
                {
                    if (Measures.Any() && value != null)
                    {
                        foreach (var m in Measures)
                        {
                            m.IsSelected = value;
                        }
                    }
                    else if (_table != null)
                    {
                        var selected = _table?.Measures.Count(m => m.IsSelected ?? false) ?? 0;

                        if (selected == 0)
                        {
                            _table.IsSelected = false;
                        }
                        else if (selected == _table.Measures.Count)
                        {
                            _table.IsSelected = true;
                        }
                        else
                        {
                            _table.IsSelected = null;
                        }
                    }

                    _parent.RecalculateSelectedCount();
                }
            }
        }

        public bool IsThreeState  => _table == null;

        public string Name { get; set; }

        public string Formula { get; set; }

        public ObservableCollection<TreeItem> Measures { get; } = new ObservableCollection<TreeItem>();
    }
}
