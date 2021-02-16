using Sqlbi.Bravo.UI.DataModel;
using System.Windows;
using System.Windows.Controls;

namespace Sqlbi.Bravo.UI.Controls
{
    public partial class WarningWithTooltip : UserControl
    {
        public WarningWithTooltip() => InitializeComponent();

        private void LearnMoreClicked(object sender, RoutedEventArgs e)
            => Views.ShellView.Instance.ShowMediaDialog(new ColumnOptimizationHelp());
    }
}
