using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace Sqlbi.Bravo.UI.Views
{
    public partial class ShellView : MetroWindow
    {
        public ShellView() => InitializeComponent();

        private void OptionsItemClick(object sender, ItemClickEventArgs args)
        {
            Dispatcher.VerifyAccess();

            ////var _customDialog = new CustomDialog();
            ////var sc = new SettingsControl();
            ////sc.CloseButton.Click += (s, e) => this.HideMetroDialogAsync(_customDialog);
            ////_customDialog.Content = sc;
            ////await this.ShowMetroDialogAsync(_customDialog);
        }

        private void MenuItemClick(object sender, ItemClickEventArgs args)
        {

        }
    }
}
