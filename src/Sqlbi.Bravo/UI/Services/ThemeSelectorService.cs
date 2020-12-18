using ControlzEx.Theming;
using MahApps.Metro.Theming;
using Sqlbi.Bravo.UI.Services.Interfaces;
using System;
using System.Windows;

namespace Sqlbi.Bravo.UI.Services
{
    public class ThemeSelectorService : IThemeSelectorService
    {
        private const string CustomDarkThemePath = "pack://application:,,,/UI/Theme/Dark.Red.xaml";
        private const string CustomLightThemePath = "pack://application:,,,/UI/Theme/Light.Red.xaml";

        public ThemeSelectorService()
        {
        }

        public void InitializeTheme(string themeName)
        {
            ThemeManager.Current.AddLibraryTheme(new LibraryTheme(new Uri(CustomDarkThemePath), MahAppsLibraryThemeProvider.DefaultInstance));
            ThemeManager.Current.AddLibraryTheme(new LibraryTheme(new Uri(CustomLightThemePath), MahAppsLibraryThemeProvider.DefaultInstance));

            SetTheme(themeName);
        }

        public void SetTheme(string themeName)
        {
            if (themeName.Equals("Default", StringComparison.InvariantCultureIgnoreCase))
            {
                // Forcibly match the system theme
                // Relying on `ThemeSyncMode.SyncWithAppMode` won't pick up the custom themes on first launch
                SetTheme(WindowsThemeHelper.GetWindowsBaseColor());
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
