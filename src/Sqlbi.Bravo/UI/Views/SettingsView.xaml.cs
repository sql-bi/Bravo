using MahApps.Metro.SimpleChildWindow;
using Sqlbi.Bravo.UI.Framework.Interfaces;
using Sqlbi.Bravo.UI.ViewModels;
using System.Security;
using System.Windows;

namespace Sqlbi.Bravo.UI.Views
{
    public partial class SettingsView : ChildWindow, ISecurePassword
    {
        public SettingsView() => InitializeComponent();

        public SecureString SecurePassword => ProxyPassword.SecurePassword;

        private void OkClicked(object sender, RoutedEventArgs e)
        {
            (DataContext as SettingsViewModel).SaveCommand.Execute(null);
            _ = Close(); 
        }
    }
}
