using Sqlbi.Bravo.Infrastructure.Windows.Interop;
using System;
using System.Runtime.InteropServices;

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

        //[DllImport("uxtheme.dll", EntryPoint = "#132")]
        //private static extern bool ShouldAppsUseDarkMode(bool allow);

        //[DllImport("uxtheme.dll", EntryPoint = "#133")]
        //private static extern bool AllowDarkModeForWindow(IntPtr hWnd, bool allow);

        [DllImport("uxtheme.dll", EntryPoint = "#135")]
        private static extern bool AllowDarkModeForApp(bool allow); // ordinal 135, in 1809 ( OS build 17763 )

        [DllImport("uxtheme.dll", EntryPoint = "#135")]
        [return: MarshalAs(UnmanagedType.U4)]
        private static extern PreferredAppMode SetPreferredAppMode(PreferredAppMode mode); // ordinal 135, in 1903 ( OS build 18362 )

        private static bool IsSupported()
        {
            var version = Environment.OSVersion.Version;

            // Windows 10 release information https://docs.microsoft.com/en-us/windows/release-health/release-information
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

        public static void SetStartupTheme(bool useDark)
        {
            if (IsSupported())
            {
                // No need to send any messges here (i.e. WM_THEMECHANGED)
                SetDarkMode(enabled: useDark);
            }
        }

        public static void ChangeTheme(IntPtr hWnd, bool useDark)
        {
            if (IsSupported())
            {
                SetDarkMode(enabled: useDark);
                NativeMethods.SendMessage(hWnd, WM_THEMECHANGED, IntPtr.Zero, IntPtr.Zero);
            }
        }
    }
}
