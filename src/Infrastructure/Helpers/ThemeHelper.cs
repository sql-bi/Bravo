namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
    using Sqlbi.Bravo.Infrastructure.Windows.Interop;
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    internal static class ThemeHelper
    {
        private static readonly Version Windows10Version1809 = new(10, 0, 17763);
        //private static readonly Version Windows10Version1909 = new(10, 0, 18363);
        //private static readonly Version Windows10Version21H1 = new(10, 0, 19043);
        private static readonly Version Windows10Version21H2 = new(10, 0, 22000);
        private static readonly bool IsWindows10Version1809 = Environment.OSVersion.Version == Windows10Version1809;
        //private static readonly bool IsWindows10Version1909OrNewer = Environment.OSVersion.Version >= Windows10Version1909;
        //private static readonly bool IsWindows10Version21H1OrNewer = Environment.OSVersion.Version >= Windows10Version21H1;
        private static readonly bool IsWindows10Version21H2OrNewer = Environment.OSVersion.Version >= Windows10Version21H2;
        private static readonly bool IsDarkModeSupported = Environment.OSVersion.Version >= Windows10Version1809;

        public static void InitializeTheme(IntPtr hWnd, ThemeType theme)
        {
            if (IsDarkModeSupported)
            {
                if (IsWindows10Version1809)
                {
                    _ = Uxtheme.AllowDarkModeForApp(allow: true);
                }
                else
                {
                    _ = Uxtheme.SetPreferredAppMode(mode: Uxtheme.PreferredAppMode.AllowDark);
                }

                Uxtheme.RefreshImmersiveColorPolicyState();

                RefreshNonClientArea(hWnd, theme);
            }
        }

        public static void ChangeTheme(ThemeType theme)
        {
            if (IsDarkModeSupported)
            {
                var hWnd = ProcessHelper.GetCurrentProcessMainWindowHandle();

                ChangeTheme(hWnd, theme);
            }
        }

        public static void ChangeTheme(IntPtr hWnd, ThemeType theme)
        {
            if (IsDarkModeSupported)
            {
                RefreshNonClientArea(hWnd, theme);
            }
        }

        public static bool ShouldUseDarkMode(ThemeType theme)
        {
            if (IsDarkModeSupported /* && !SystemInformation.HighContrast */)
            {
                bool useDarkMode;

                if (theme == ThemeType.Auto)
                {
                    useDarkMode = Uxtheme.ShouldAppsUseDarkMode();
                }
                else
                {
                    useDarkMode = theme == ThemeType.Dark; // Uxtheme.IsDarkModeAllowedForWindow(hWnd);
                }

                return useDarkMode;
            }

            return false;
        }

        private static void RefreshNonClientArea(IntPtr hWnd, ThemeType theme)
        {
            Debug.Assert(IsDarkModeSupported);

            var useDarkMode = ShouldUseDarkMode(theme);

            if (IsWindows10Version21H2OrNewer) // >= Windows 11
            {
                // Undocumented DWMWINDOWATTRIBUTE supported on Windows 11 only
                const Dwmapi.DWMWINDOWATTRIBUTE DWMWA_CAPTION_COLOR = (Dwmapi.DWMWINDOWATTRIBUTE)35;
                //const Dwmapi.DWMWINDOWATTRIBUTE DWMWA_TEXT_COLOR = (Dwmapi.DWMWINDOWATTRIBUTE)36;

                COLORREF color = useDarkMode ? AppEnvironment.ThemeColorDark : AppEnvironment.ThemeColorLight;
                GCHandle pinnedColor = GCHandle.Alloc(color, GCHandleType.Pinned);
                try
                {
                    var dwAttribute = DWMWA_CAPTION_COLOR;
                    var pvAttribute = pinnedColor.AddrOfPinnedObject();
                    var cbAttribute = Marshal.SizeOf(color);

                    _ = Dwmapi.DwmSetWindowAttribute(hWnd, dwAttribute, pvAttribute, cbAttribute);
                }
                finally
                {
                    if (pinnedColor.IsAllocated)
                        pinnedColor.Free();
                }
            }
            else // if (IsWindows10Version1909OrNewer)
            {
                var size = Marshal.SizeOf(useDarkMode);
                var ptr = Marshal.AllocHGlobal(size);
                try
                {
                    Marshal.WriteInt32(ptr, ofs: 0, val: useDarkMode ? 1 : 0);

                    var data = new User32.WINDOWCOMPOSITIONATTRIBDATA
                    {
                        Attrib = User32.WINDOWCOMPOSITIONATTRIB.WCA_USEDARKMODECOLORS,
                        pvData = ptr,
                        cbData = size
                    };

                    _ = User32.SetWindowCompositionAttribute(hWnd, ref data);

                    // >> HACK: To force non-client area repainting
                    // >>   Win10 20H2 build 19042
                    var forceActive = new IntPtr(1); /* TRUE */
                    var forceInactive = IntPtr.Zero; /* FALSE */
                    _ = User32.SendMessage(hWnd, User32.WindowMessage.WM_NCACTIVATE, wParam: forceInactive, IntPtr.Zero);
                    _ = User32.SendMessage(hWnd, User32.WindowMessage.WM_NCACTIVATE, wParam: forceActive, IntPtr.Zero);
                    // << HACK
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }
            //else
            //{
            //    const Dwmapi.DWMWINDOWATTRIBUTE DWMWA_USE_IMMERSIVE_DARK_MODE_19 = (Dwmapi.DWMWINDOWATTRIBUTE)19;
            //    const Dwmapi.DWMWINDOWATTRIBUTE DWMWA_USE_IMMERSIVE_DARK_MODE_20 = (Dwmapi.DWMWINDOWATTRIBUTE)20;

            //    var dwAttribute = DWMWA_USE_IMMERSIVE_DARK_MODE_20;
            //    var pvAttribute = new IntPtr(useDarkMode ? 1 : 0);
            //    var cbAttribute = Marshal.SizeOf(pvAttribute);

            //    var hresult = Dwmapi.DwmSetWindowAttribute(hWnd, dwAttribute, pvAttribute, cbAttribute);
            //    if (hresult != HRESULT.S_OK)
            //    {
            //        dwAttribute = DWMWA_USE_IMMERSIVE_DARK_MODE_19;

            //        _ = Dwmapi.DwmSetWindowAttribute(hWnd, dwAttribute, pvAttribute, cbAttribute);
            //    }
            //}
        }
    }
}
