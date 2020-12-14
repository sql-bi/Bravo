using MahApps.Metro.SimpleChildWindow;
using Sqlbi.Bravo.UI.Framework.Interfaces;
using System.Security;
using System.Windows;

namespace Sqlbi.Bravo.UI.Views
{
    public partial class SettingsView : ChildWindow, ISecurePassword
    {
        public SettingsView() => InitializeComponent();

        public SecureString SecurePassword => ProxyPassword.SecurePassword;

        private void OkClicked(object sender, RoutedEventArgs e) => _ = Close();
    }
}
