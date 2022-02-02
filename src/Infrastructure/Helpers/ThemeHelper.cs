namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
    using Sqlbi.Bravo.Infrastructure.Windows.Interop;
    using System;
    using System.Runtime.InteropServices;

    internal static class ThemeHelper
    {
        private static readonly Version Windows11Version21H2 = new(10, 0, 22000);
        private static readonly Version Windows10Version1809 = new(10, 0, 17763);
        private static readonly bool IsWindows10Version1809 = Environment.OSVersion.Version == Windows10Version1809;
        private static readonly bool IsWindows11OrNewer = Environment.OSVersion.Version >= Windows11Version21H2;
        private static readonly bool IsDarkModeSupported = Environment.OSVersion.Version >= Windows10Version1809;

        public static void AllowDarkModeForApp(bool allow, bool force)
        {
            if (IsDarkModeSupported)
            {
                if (IsWindows10Version1809)
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

        public static void ChangeStartupTheme(IntPtr hWnd, ThemeType theme)
        {
            if (IsDarkModeSupported && theme != ThemeType.Auto)
            {
                // No need to send WM_THEMECHANGED message on app startup
                ChangeTheme(theme, notifyThemeChanged: false);
            }

            UpdateNonClientAreaTheme(hWnd, theme);
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
                    if (retval == IntPtr.Zero)
                    {
                        UpdateNonClientAreaTheme(hWnd, theme);
                    }
                }
            }
        }

        public static void UpdateNonClientAreaTheme(IntPtr hWnd, ThemeType theme)
        {
            // Undocumented custom caption/text DWMWINDOWATTRIBUTE supported on Windows 11
            const Dwmapi.DWMWINDOWATTRIBUTE DWMWA_CAPTION_COLOR = (Dwmapi.DWMWINDOWATTRIBUTE)35;
            //const Dwmapi.DWMWINDOWATTRIBUTE DWMWA_TEXT_COLOR = (Dwmapi.DWMWINDOWATTRIBUTE)36;

            // Only supported on Windows 11
            if (IsWindows11OrNewer)
            {
                var useDark = theme switch
                {
                    ThemeType.Auto => Uxtheme.ShouldAppsUseDarkMode(),
                    _ => Uxtheme.IsDarkModeAllowedForApp(hWnd),
                };

                COLORREF color = useDark
                    ? AppEnvironment.ThemeColorDark 
                    : AppEnvironment.ThemeColorLight;

                var pinned = GCHandle.Alloc(color, GCHandleType.Pinned);
                try
                {
                    var dwAttribute = DWMWA_CAPTION_COLOR;
                    var pvAttribute = pinned.AddrOfPinnedObject();
                    var cbAttribute = Marshal.SizeOf(color);

                    _ = Dwmapi.DwmSetWindowAttribute(hWnd, dwAttribute, pvAttribute, cbAttribute);
                }
                finally
                {
                    if (pinned.IsAllocated)
                        pinned.Free();
                }
            }
        }
    }
}
