using Sqlbi.Bravo.Infrastructure.Windows.Interop;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

#nullable disable

namespace Bravo.Infrastructure.Windows.Interop
{
    ///***************************************************************************
    ///***************************************************************************
    /// <remarks>
    ///     This class uses undocumented APIs introduced in Windows 10 build 1809
    /// </remarks>
    ///***************************************************************************
    ///***************************************************************************
    internal static class Win32UxTheme
    {
        private const int WM_THEMECHANGED = 0x031A;

        private enum PreferredAppMode
        {
            Default,
            AllowDark,
            ForceDark,
            ForceLight,
            Max
        };

        /*
         * OS version 1809 ( OS build 17763 )
         */

        [DllImport("uxtheme.dll", EntryPoint = "#132")]
        private static extern bool ShouldAppsUseDarkMode();

        //[DllImport("uxtheme.dll", EntryPoint = "#133")]
        //private static extern bool AllowDarkModeForWindow(IntPtr hWnd, bool allow);

        [DllImport("uxtheme.dll", EntryPoint = "#135")]
        private static extern bool AllowDarkModeForApp(bool allow); // ordinal *** 135 ***, in 1809 ( OS build 17763 )

        //[DllImport("uxtheme.dll", EntryPoint = "#137")]
        //private static extern bool IsDarkModeAllowedForWindow(IntPtr hWnd);

        /*
         * OS version 1903 ( OS build 18362 )
         */

        [DllImport("uxtheme.dll", EntryPoint = "#135")]
        [return: MarshalAs(UnmanagedType.U4)]
        private static extern PreferredAppMode SetPreferredAppMode(PreferredAppMode mode); // ordinal *** 135 *** , in 1903 ( OS build 18362 )

        [DllImport("uxtheme.dll", EntryPoint = "#139")]
        private static extern bool IsDarkModeAllowedForApp(IntPtr hWnd);

        /* */

        private static bool IsThemingSupported()
        {
            // Windows 10 release information https://docs.microsoft.com/en-us/windows/release-health/release-information

            var version = Environment.OSVersion.Version;

            if (version.Major == 10 && version.Build >= 17763 /* Version 1809 */)
                return true;

            if (version.Major > 10)
                return true;

            return false;
        }

        private static void SetDarkMode(bool enabled)
        {
            var version = Environment.OSVersion.Version;

            if (version.Major == 10 && version.Build < 18362 /* Version 1903 */)
            {
                // Ordinal entry point ***135*** in 1809 (OS build 17763)
                AllowDarkModeForApp(enabled);
            }
            else
            {
                // Ordinal entry point ***135*** in 1903 (OS build 18362)
                SetPreferredAppMode(enabled ? PreferredAppMode.ForceDark : PreferredAppMode.ForceLight);
            }
        }

        public static void SetStartupMode(bool useDark)
        {
            if (IsThemingSupported())
            {
                // No need to send any messges here (i.e. WM_THEMECHANGED)
                SetDarkMode(enabled: useDark);
            }
        }

        public static void ChangeMode(IntPtr hWnd, bool useDark)
        {
            if (IsThemingSupported())
            {
                SetDarkMode(enabled: useDark);
                NativeMethods.SendMessage(hWnd, WM_THEMECHANGED, IntPtr.Zero, IntPtr.Zero);
            }
        }

        public static bool IsDarkModeEnabled()
        {
            if (IsThemingSupported() && !SystemInformation.HighContrast)
            {
                return ShouldAppsUseDarkMode();
            }

            return false;
        }
    }
}
