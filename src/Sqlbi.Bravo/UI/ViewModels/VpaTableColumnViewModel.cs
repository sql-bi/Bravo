using Dax.ViewModel;
using System.Collections.Generic;

namespace Sqlbi.Bravo.UI.ViewModels
{
    internal class VpaTableColumnViewModel : VpaColumnViewModel
    {
        private bool _isSelected;

        public VpaTableColumnViewModel(AnalyzeModelViewModel parent)
            : base(parent)
        {
        }

        public VpaTableColumnViewModel(AnalyzeModelViewModel parent, VpaColumn vpaColumn, bool isTable)
            : base(parent, vpaColumn)
        {
            IsTable = isTable;
        }

        public override bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetProperty(ref _isSelected, value))
                {
                    foreach (var col in Columns)
                    {
                        col.IsSelected = value;
                    }

                    base.IsSelected = value;
                }
            }
        }

        public bool IsTable { get; set; }

        public List<VpaColumnViewModel> Columns { get; set; } = new List<VpaColumnViewModel>();
    }
}
