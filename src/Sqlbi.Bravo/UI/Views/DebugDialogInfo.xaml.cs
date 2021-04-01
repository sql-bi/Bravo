using MahApps.Metro.Controls;
using System;

namespace Sqlbi.Bravo.UI.Views
{
    public partial class DebugDialogInfo : MetroWindow
    {
        public DebugDialogInfo() => InitializeComponent();

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
