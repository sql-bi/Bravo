using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Sqlbi.Bravo.UI.Controls
{
    public partial class DaxFormatterIcon : UserControl
    {
        public DaxFormatterIcon()
        {
            InitializeComponent();
            DataContext = this;
        }

        public Brush ForegroundBrush
        {
            get => (Brush)GetValue(ForegroundBrushProperty);
            set => SetValue(ForegroundBrushProperty, value);
        }

        public static readonly DependencyProperty ForegroundBrushProperty =
            DependencyProperty.Register(
                "ForegroundBrush",
                typeof(Brush),
                typeof(DaxFormatterIcon),
                new PropertyMetadata(Brushes.Red));
    }
}
