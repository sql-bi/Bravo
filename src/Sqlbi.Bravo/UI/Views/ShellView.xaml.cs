using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.SimpleChildWindow;
using Sqlbi.Bravo.Core.Settings.Interfaces;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Sqlbi.Bravo.UI.ViewModels;
using Sqlbi.Bravo.UI.DataModel;
using System.Linq;

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

#if DEBUG
            if (settings.Runtime.IsExecutedAsExternalTool)
#else
            if (settings.Runtime.IsExecutedAsExternalToolForPowerBIDesktop)
#endif
            {
                (DataContext as ShellViewModel).LaunchedViaPowerBIDesktop(settings.Runtime.ParentProcessMainWindowTitle);
            }
        }

        internal async Task ShowMediaDialog(IInAppMediaOption mediaOptions)
        {
            await this.ShowChildWindowAsync(new MediaDialog(mediaOptions)
            {
                ChildWindowHeight = ActualHeight - 100,
                ChildWindowWidth = ActualWidth - 150
            });
        }

        internal async Task ShowSettings()
        {
            await this.ShowChildWindowAsync(new SettingsView()
            {
                ChildWindowHeight = ActualHeight - 100,
                ChildWindowWidth = ActualWidth - 150
            });
        }

        private void AddTabClicked(object sender, System.Windows.RoutedEventArgs e)
        {
            var vm = DataContext as ShellViewModel;
            vm.Tabs.Add((TabItemViewModel)App.ServiceProvider.GetRequiredService(typeof(TabItemViewModel)));

            // Select added item so it is made visible
            vm.SelectedTab = vm.Tabs.Last();
        }
    }
}
