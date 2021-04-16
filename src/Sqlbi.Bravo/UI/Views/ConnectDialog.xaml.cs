using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;

namespace Sqlbi.Bravo.UI.Views
{
    public partial class ConnectDialog : MetroWindow
    {
        public ConnectDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        public ObservableCollection<string> ActiveDesktops { get; set; } = new ObservableCollection<string>();

        public int ResultIndex { get; private set; } = -1;

        public void ShowDesktopOptions(List<string> options)
        {
            ActiveDesktops.Clear();

            foreach (var item in options)
            {
                ActiveDesktops.Add(item);
            }

            // TODO: show empty list message
            if (options.Count > 0)
            {
                ActiveDesktopList.SelectedIndex = 0;
            }
            else
            {
                EmptyDesktopListMessage.Visibility = Visibility.Visible;
            }

            TheTabControl.SelectedIndex = 0;
            DesktopTab.IsEnabled = true;
            DatasetsTab.IsEnabled = false;
        }

        private void AttachClicked(object sender, RoutedEventArgs e)
        {
            ResultIndex = ActiveDesktopList.SelectedIndex;
            DialogResult = true;
            Close();
        }

        private void ConnectClicked(object sender, RoutedEventArgs e)
        {

        }
    }
}
