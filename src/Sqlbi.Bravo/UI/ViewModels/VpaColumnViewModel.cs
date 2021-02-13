using Dax.ViewModel;
using Sqlbi.Bravo.UI.Controls;
using Sqlbi.Bravo.UI.Framework.ViewModels;
using System.Windows;
using System.Windows.Media;

namespace Sqlbi.Bravo.UI.ViewModels
{
    internal class VpaColumnViewModel : BaseViewModel, ITreeMapInfo
    {
        private readonly AnalyzeModelViewModel parent;
        private bool _isSelected;

        // Coupling this VM to the parent is the simplest way to track totals based on selection
        public VpaColumnViewModel(AnalyzeModelViewModel parent) => this.parent = parent;

        public VpaColumnViewModel(AnalyzeModelViewModel parent, VpaColumn vpaColumn)
            : this(parent)
        {
            // TODO REQUIREMENTS: change this to use .IsReferenced (or similar) once available.
            // Currently using IsHidden as a proxy so rest of functionality can be implemented.
            IsRequired = vpaColumn.IsHidden;
            ColumnName = vpaColumn.ColumnName;
            TableName = vpaColumn.Table.TableName;
            Cardinality = vpaColumn.ColumnCardinality;
            TotalSize = vpaColumn.TotalSize;
            PercentageDatabase = vpaColumn.PercentageDatabase;
        }

        public virtual bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetProperty(ref _isSelected, value)){
                    parent.OnPropertyChanged(nameof(AnalyzeModelViewModel.SelectedColumnCount));
                    parent.OnPropertyChanged(nameof(AnalyzeModelViewModel.SelectedColumnSize));
                    parent.OnPropertyChanged(nameof(AnalyzeModelViewModel.SelectedColumnWeight));
                    OnPropertyChanged(nameof(OverlayVisibility));
                }
            }
        }

        public bool IsRequired { get; set; }

        public string ColumnName { get; set; }

        public string TableName { get; set; }

        public long Cardinality { get; set; }

        public long TotalSize { get; set; }

        public double PercentageDatabase { get; set; }

        public long Size => TotalSize;

        public Color RectangleColor => parent.GetTableColor(TableName);

        public string ToolTipText => $"[{TableName}].[{ColumnName}]";

        public Visibility OverlayVisibility => IsSelected ? Visibility.Collapsed : Visibility.Visible;
    }
}
