using MahApps.Metro.Controls;
using MahApps.Metro.SimpleChildWindow;
using Sqlbi.Bravo.Core.Settings.Interfaces;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Sqlbi.Bravo.UI.ViewModels;
using Sqlbi.Bravo.UI.DataModel;
using System.Linq;
using System.Windows.Interop;
using System.Windows;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Sqlbi.Bravo.UI.Views
{
    public partial class ShellView : MetroWindow
    {
        public static ShellView Instance { get; private set; }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == MessageHelper.WM_COPYDATA)
            {
                var connInfo = new ConnectionInfo();
                try
                {
                    var cp = (MessageHelper.CopyDataStruct)Marshal.PtrToStructure(lParam, typeof(MessageHelper.CopyDataStruct));
                    if (cp.cbData == Marshal.SizeOf(connInfo))
                    {
                        connInfo = (ConnectionInfo)Marshal.PtrToStructure(cp.lpData, typeof(ConnectionInfo));

                        var details = connInfo.Details;

                        var deats = details.Split('|');

                        System.Diagnostics.Debug.WriteLine($"DatabaseName = '{deats[0]}'");
                        System.Diagnostics.Debug.WriteLine($"ServerName = '{deats[1]}'");
                        System.Diagnostics.Debug.WriteLine($"ParentProcessName = '{deats[2]}'");
                        //System.Diagnostics.Debug.WriteLine(connInfo.DatabaseName);
                        //System.Diagnostics.Debug.WriteLine(connInfo.ServerName);
                        //System.Diagnostics.Debug.WriteLine(connInfo.ParentProcessName);
                    }
                }
                catch (Exception e)
                {
                    var logger = App.ServiceProvider.GetRequiredService<ILogger<ShellView>>();
                    logger.LogError(e, "Unable to decode connection settings from another app instance");
                }
                handled = true;
            }

            return IntPtr.Zero;
        }

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
