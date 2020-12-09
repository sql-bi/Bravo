using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.SimpleChildWindow;
using Sqlbi.Bravo.Core.Settings.Interfaces;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Sqlbi.Bravo.UI.ViewModels;

namespace Sqlbi.Bravo.UI.Views
{
    public partial class ShellView : MetroWindow
    {
        public static ShellView Instance { get; private set; }

        public ShellView()
        {
            InitializeComponent();
            Instance = this;

            var settings = App.ServiceProvider.GetService<IGlobalSettingsProviderService>();
            //if (settings.Runtime.IsExecutedAsExternalToolForPowerBIDesktop)
            if (settings.Runtime.IsExecutedAsExternalTool)
                {
                // TODO switch to appropriate mode
                (this.DataContext as ShellViewModel).LaunchedViaPowerBIDesktop();
            }
        }

        internal async Task ShowMediaDialog(object dialogContent)
        {
            //var _customDialog = new CustomDialog();
            //var sc = new MediaDialog(new HowToUseBravoHelp());
            //sc.CloseButton.Click += (s, e) => ShellView.Instance.HideMetroDialogAsync(_customDialog);
            //_customDialog.Content = sc;
            //await ShellView.Instance.ShowMetroDialogAsync(_customDialog);
        }

        internal async Task ShowSettings()
        {
            await this.ShowChildWindowAsync(new SettingsView()
            {
                ChildWindowHeight = ActualHeight - 100,
                ChildWindowWidth = ActualWidth - 150
            });
            //var _customDialog = new CustomDialog();
            //var sc = new SettingsView();
            //sc.CloseButton.Click += (s, e) => ShellView.Instance.HideMetroDialogAsync(_customDialog);
            //_customDialog.Content = sc;
            //await ShellView.Instance.ShowMetroDialogAsync(_customDialog);
        }
    }
}
