namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
    using Sqlbi.Bravo.Infrastructure.Windows.Interop;
    using System;
    using System.Windows.Forms;

    internal static class ThemeHelper
    {
        private static readonly int Windows10Version1809BuildNumber = 17763;
        private static readonly int Windows10Version1909BuildNumber = 18362;
        private static readonly bool IsWindows10OSVersion1809OrLower = Environment.OSVersion.Version.Major == 10 && Environment.OSVersion.Version.Build < Windows10Version1909BuildNumber;
        private static readonly bool IsDarkModeSupported = Environment.OSVersion.Version.Major > 10 || (Environment.OSVersion.Version.Major == 10 && Environment.OSVersion.Version.Build >= Windows10Version1809BuildNumber);

        public static void AllowDarkModeForApp(bool allow, bool force)
        {
            if (IsDarkModeSupported)
            {
                if (IsWindows10OSVersion1809OrLower)
                {
                    // Ordinal entry point *** 135 *** in 1809 (OS build 17763)
                    _ = Uxtheme.AllowDarkModeForApp(allow);
                }
                else
                {
                    // Ordinal entry point *** 135 *** in 1903 (OS build 18362) and later
                    if (force)
                    {
                        _ = Uxtheme.SetPreferredAppMode(allow ? Uxtheme.PreferredAppMode.ForceDark : Uxtheme.PreferredAppMode.ForceLight);
                    }
                    else
                    {
                        _ = Uxtheme.SetPreferredAppMode(allow ? Uxtheme.PreferredAppMode.AllowDark : Uxtheme.PreferredAppMode.Default);
                    }
                }

                Uxtheme.RefreshImmersiveColorPolicyState();
            }
        }

        public static void AllowDarkModeForWindow(IntPtr hWnd, bool allow = true)
        {
            if (IsDarkModeSupported)
            {
                _ = Uxtheme.AllowDarkModeForWindow(hWnd, allow);
                // TODO: RefreshTitleBarThemeColor(hWnd);
            }
        }

        public static void ChangeStartupTheme(ThemeType theme)
        {
            if (IsDarkModeSupported && theme != ThemeType.Auto)
            {
                // No need to send WM_THEMECHANGED message on app startup
                ChangeTheme(theme, notifyThemeChanged: false);
            }
        }

        public static void ChangeTheme(ThemeType theme, bool notifyThemeChanged = true)
        {
            if (IsDarkModeSupported)
            {
                switch (theme)
                {
                    case ThemeType.Light:
                    case ThemeType.Dark:
                        AllowDarkModeForApp(allow: theme == ThemeType.Dark, force: theme == ThemeType.Dark);
                        break;
                    case ThemeType.Auto:
                        AllowDarkModeForApp(allow: true, force: false);
                        break;
                }

                if (notifyThemeChanged)
                {
                    var hWnd = ProcessHelper.GetCurrentProcessMainWindowHandle();
                    var retval = User32.SendMessage(hWnd, WindowMessage.WM_THEMECHANGED, IntPtr.Zero, IntPtr.Zero);
                    if (retval != IntPtr.Zero)
                    { }
                }
            }
        }

        public static ThemeType GetWindowsSettingsAppTheme()
        {
            if (IsDarkModeSupported && !SystemInformation.HighContrast)
            {
                if (Uxtheme.ShouldAppsUseDarkMode())
                {
                    return ThemeType.Dark;
                }
            }

            return ThemeType.Light;
        }
    }
}
