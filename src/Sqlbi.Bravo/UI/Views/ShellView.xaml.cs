using MahApps.Metro.Controls;
using MahApps.Metro.SimpleChildWindow;
using Sqlbi.Bravo.Core.Settings.Interfaces;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Sqlbi.Bravo.UI.ViewModels;
using Sqlbi.Bravo.UI.DataModel;
using System.Windows.Interop;
using System.Windows;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Core.Settings;

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
                var connInfo = new MessageHelper.ConnectionInfo();
                try
                {
                    var cp = (MessageHelper.CopyDataStruct)Marshal.PtrToStructure(lParam, typeof(MessageHelper.CopyDataStruct));
                    if (cp.cbData == Marshal.SizeOf(connInfo))
                    {
                        connInfo = (MessageHelper.ConnectionInfo)Marshal.PtrToStructure(cp.lpData, typeof(MessageHelper.ConnectionInfo));

                        var details = MessageHelper.ExtractDetails(connInfo);

                        System.Diagnostics.Debug.WriteLine($"DatabaseName = '{details.DbName}'");
                        System.Diagnostics.Debug.WriteLine($"ServerName = '{details.ServerName}'");
                        System.Diagnostics.Debug.WriteLine($"ParentProcessName = '{details.ParentProcName}'");
                        System.Diagnostics.Debug.WriteLine($"ParentWindowTitle = '{details.ParentWindowTitle}'");

                        var runtimeSummary = new RuntimeSummary
                        {
                            DatabaseName = details.DbName,
                            IsExecutedAsExternalTool = true,
                            ParentProcessMainWindowTitle = details.ParentWindowTitle,
                            ParentProcessName = details.ParentProcName,
                            ServerName = details.ServerName,
                        };

                        var vm = DataContext as ShellViewModel;

                        vm.AddNewTab(BiConnectionType.ActivePowerBiWindow, vm.SelectedItem.NavigationPage, runtimeSummary);
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
            (DataContext as ShellViewModel).AddNewTab();
        }
    }
}
