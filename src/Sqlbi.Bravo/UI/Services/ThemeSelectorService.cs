using ControlzEx.Theming;
using MahApps.Metro.Theming;
using Sqlbi.Bravo.Core;
using Sqlbi.Bravo.UI.DataModel;
using Sqlbi.Bravo.UI.Services.Interfaces;
using System;
using System.Windows;

namespace Sqlbi.Bravo.UI.Services
{
    public class ThemeSelectorService : IThemeSelectorService
    {
        public ThemeSelectorService()
        {
        }

        public void InitializeTheme(string themeName) => SetTheme(themeName);

        public void SetTheme(string themeName)
        {

            if (themeName.Equals(AppConstants.ApplicationSettingsDefaultThemeName, StringComparison.InvariantCultureIgnoreCase))
            {
                // Forcibly match the system theme
                // Relying on `ThemeSyncMode.SyncWithAppMode` won't pick up the custom themes on first launch
                SetTheme(WindowsThemeHelper.GetWindowsBaseColor());
            }
            else
            {
                ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAccent;
                ThemeManager.Current.SyncTheme();
                ThemeManager.Current.ChangeTheme(Application.Current, $"{themeName}.Red");
            }
        }
    }
}
