using Dax.ViewModel;
using Sqlbi.Bravo.UI.Controls;
using Sqlbi.Bravo.UI.Framework.ViewModels;
using System.Windows;
using System.Windows.Media;

namespace Sqlbi.Bravo.UI.ViewModels
{
    internal class VpaColumnViewModel : BaseViewModel, ITreeMapInfo
    {
        private readonly AnalyzeModelViewModel _parent;
        private bool? _isSelected = false;

        // Coupling this VM to the parent is the simplest way to track totals based on selection
        public VpaColumnViewModel(AnalyzeModelViewModel parent, VpaColumn vpaColumn)
        {
            _parent = parent;
            VpaColumn = vpaColumn;
            IsUnused = !vpaColumn.IsReferenced;
            ColumnName = vpaColumn.ColumnName;
            TableName = vpaColumn.Table.TableName;
            Cardinality = vpaColumn.ColumnCardinality;
            TotalSize = vpaColumn.TotalSize;
            PercentageDatabase = vpaColumn.PercentageDatabase;
        }

        public virtual bool? IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetProperty(ref _isSelected, value)){
                    _parent.OnPropertyChanged(nameof(AnalyzeModelViewModel.SelectedColumnCount));
                    _parent.OnPropertyChanged(nameof(AnalyzeModelViewModel.SelectedColumnSize));
                    _parent.OnPropertyChanged(nameof(AnalyzeModelViewModel.SelectedColumnWeight));
                    OnPropertyChanged(nameof(OverlayVisibility));
                }
            }
        }

        public VpaColumn VpaColumn { get; }

        public bool IsUnused { get; set; }

        public string ColumnName { get; set; }

        public string TableName { get; set; }

        public long Cardinality { get; set; }

        public long TotalSize { get; set; }

        public double PercentageDatabase { get; set; }

        public long Size => TotalSize;

        public Color RectangleColor => _parent.GetTableColor(TableName);

        public string ToolTipText => $"'{TableName}'[{ColumnName}]";

        public Visibility OverlayVisibility => (IsSelected ?? false) ? Visibility.Collapsed : Visibility.Visible;
    }
}
