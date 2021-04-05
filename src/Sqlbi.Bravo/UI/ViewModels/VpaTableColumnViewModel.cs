using Dax.ViewModel;
using System.Collections.Generic;
using System.Linq;

namespace Sqlbi.Bravo.UI.ViewModels
{
    internal class VpaTableColumnViewModel : VpaColumnViewModel
    {
        private bool? _isSelected = false;
        private readonly VpaTableColumnViewModel _table;

        public VpaTableColumnViewModel(AnalyzeModelViewModel parent, VpaColumn vpaColumn, VpaTableColumnViewModel table = null)
            : base(parent, vpaColumn)
        {
            _table = table;
            IsTable = _table == null;
        }

        public bool IsThreeState { get; set; } = false;

        public override bool? IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetProperty(ref _isSelected, value))
                {
                    if (Columns.Any() && value != null)
                    {
                        foreach (var column in Columns)
                        {
                            column.IsSelected = value;
                        }
                    }
                    else if (_table != null)
                    {
                        var selected = _table?.Columns.Count((m) => m.IsSelected ?? false) ?? 0;

                        if (selected == 0)
                        {
                            _table.IsSelected = false;
                            _table.IsThreeState = false;
                        }
                        else if (selected == _table.Columns.Count)
                        {
                            _table.IsSelected = true;
                            _table.IsThreeState = false;
                        }
                        else
                        {
                            _table.IsThreeState = true;
                            _table.IsSelected = null;
                        }
                    }

                    base.IsSelected = value;
                }
            }
        }

        public bool IsTable { get; } 

        public List<VpaColumnViewModel> Columns { get; set; } = new List<VpaColumnViewModel>();
    }
}
