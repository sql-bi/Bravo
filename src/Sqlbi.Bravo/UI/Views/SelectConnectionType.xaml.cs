using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Sqlbi.Bravo.UI.Views
{
    public partial class SelectConnectionType : Page
    {
        public SelectConnectionType() => InitializeComponent();

        private void RequestNavigateHyperlink(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void HowToUseClicked(object sender, System.Windows.RoutedEventArgs e)
        {
            Dispatcher.VerifyAccess();

            //var _customDialog = new CustomDialog();
            //var sc = new MediaDialog(new HowToUseBravoHelp());
            //sc.CloseButton.Click += (s, e) => ShellWindow.Instance.HideMetroDialogAsync(_customDialog);
            //_customDialog.Content = sc;
            //await ShellWindow.Instance.ShowMetroDialogAsync(_customDialog);
        }

        private void AttachToWindowClicked(object sender, System.Windows.RoutedEventArgs e)
        {

        }

        private void UseDummyDataClicked(object sender, System.Windows.RoutedEventArgs e)
        {

        }
    }
}
