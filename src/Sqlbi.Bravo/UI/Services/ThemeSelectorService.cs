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

        public void InitializeTheme()
        {
            var theme = GetCurrentTheme();
            SetTheme(theme);
        }

        public void SetTheme(AppTheme theme)
        {
            if (theme == AppTheme.Default)
            {
                ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncAll;
                ThemeManager.Current.SyncTheme();
            }
            else
            {
                ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithHighContrast;
                ThemeManager.Current.SyncTheme();
                ThemeManager.Current.ChangeTheme(Application.Current, $"{theme}.Red", SystemParameters.HighContrast);
            }

            App.Current.Properties["Theme"] = theme.ToString();
        }

        public AppTheme GetCurrentTheme()
        {
            if (App.Current.Properties.Contains("Theme"))
            {
                var themeName = App.Current.Properties["Theme"].ToString();
                _ = Enum.TryParse(themeName, out AppTheme theme);
                return theme;
            }

            return AppTheme.Default;
        }
    }
}
