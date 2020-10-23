using Sqlbi.Bravo.UI.Framework.Interfaces;
using System.Security;
using System.Windows.Controls;

namespace Sqlbi.Bravo.UI.Views
{
    public partial class SettingsView : Page, ISecurePassword
    {
        public SettingsView() => InitializeComponent();

        public SecureString SecurePassword => ProxyPassword.SecurePassword;
    }
}
