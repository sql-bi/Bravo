using ControlzEx.Theming;
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
            if (themeName.Equals("Default", StringComparison.InvariantCultureIgnoreCase))
            {
                ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
                ThemeManager.Current.SyncTheme();
            }
            else
            {
                ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithHighContrast;
                ThemeManager.Current.SyncTheme();
                ThemeManager.Current.ChangeTheme(Application.Current, $"{themeName}.Red", SystemParameters.HighContrast);
            }
        }
    }
}
