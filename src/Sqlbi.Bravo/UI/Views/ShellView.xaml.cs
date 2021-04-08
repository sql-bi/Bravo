using MahApps.Metro.Controls;
using MahApps.Metro.SimpleChildWindow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Core.Logging;
using Sqlbi.Bravo.Core.Services.Interfaces;
using Sqlbi.Bravo.Core.Settings.Interfaces;
using Sqlbi.Bravo.Core.Windows;
using Sqlbi.Bravo.UI.DataModel;
using Sqlbi.Bravo.UI.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace Sqlbi.Bravo.UI.Views
{
    public partial class ShellView : MetroWindow
    {
        private readonly IGlobalSettingsProviderService _settings;
        private readonly ILogger _logger;

        public static ShellView Instance { get; private set; }

        private ShellViewModel ViewModel => DataContext as ShellViewModel;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProcHook);
        }

        private IntPtr WndProcHook(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_COPYDATA)
            { 
                try
                {
                    var applicationInstance = App.ServiceProvider.GetRequiredService<IApplicationInstanceService>();

                    var connectionSettings = applicationInstance.ReceiveConnectionFromSecondaryInstance(ptr: lParam);
                    if (connectionSettings != null)
                    {
                        // Creating the tab (& VMs) may not trigger the loaded event when expected
                        ViewModel.AddNewTab(BiConnectionType.ActivePowerBiWindow, ViewModel?.SelectedItem?.SubPageInTab ?? SubPage.DaxFormatter, connectionSettings);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(LogEvents.ShellViewException, ex);
                }

                handled = true;
            }

            return IntPtr.Zero;
        }

        public ShellView()
        {
            _logger = App.ServiceProvider.GetService<ILogger<ShellView>>();
            _settings = App.ServiceProvider.GetService<IGlobalSettingsProviderService>();

            InitializeComponent();

            Instance = this;
#if DEBUG
            if (_settings.Runtime.IsExecutedAsExternalTool)
#else
            if (_settings.Runtime.IsExecutedAsExternalToolForPowerBIDesktop)
#endif
            {
                ViewModel.LaunchedViaPowerBIDesktop(_settings.Runtime.ParentProcessMainWindowTitle);
            }
        }

        internal void ShowMediaDialog(IInAppMediaOption mediaOptions)
        {
            var mediaWindow = new MediaDialog(mediaOptions);
            mediaWindow.Show();
        }

        internal async Task ShowSettings()
        {
            _logger.Information(LogEvents.ShellViewAction, "{@Details}", new object[] { new
            {
                Action = "ShowSettings"
            }});

            await this.ShowChildWindowAsync(new SettingsView()
            {
                ChildWindowHeight = ActualHeight - 100,
                ChildWindowWidth = ActualWidth - 150
            });
        }

        internal void ShowDebugInfo()
        {
            var debugInfo = new DebugInfo();
            debugInfo.Show();
        }

        private void AddTabClicked(object sender, RoutedEventArgs e) => ViewModel.AddNewTab();

        // When the selected tab changes update the selected menu item accordingly
        private void OnSelectedTabChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedTab = ViewModel.SelectedTab;
            if (selectedTab != null)
            {
                if (selectedTab.ShowSelectConnection)
                {
                    ViewModel.SelectedItem = null;
                    hamburgerMenu.SelectedIndex = -1;
                }
                else if (selectedTab.ShowDaxFormatter)
                {
                    Select("Format DAX");
                }
                else if (selectedTab.ShowAnalyzeModel)
                {
                    Select("Analyze Model");
                }
            }

            //_logger.Information(LogEvents.ShellViewAction, "{@Details}", new object[] { new
            //{
            //    Action = "SelectedTabChanged",
            //    //Name = ViewModel.SelectedItem?.Name ?? "?"
            //}});

            void Select(string menuItemName)
            {
                // Update selected menu item
                ViewModel.SelectedItem = ViewModel.MenuItems.FirstOrDefault((i) => i.Name == menuItemName);

                // Binding doesn't updated the selected indicator - but this does
                for (var i = 0; i < ViewModel.MenuItems.Count; i++)
                {
                    if (ViewModel.MenuItems[i].Name == menuItemName)
                    {
                        hamburgerMenu.SelectedIndex = i;
                        break;
                    }
                }
            }
        }
    }
}
