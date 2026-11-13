using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;

namespace Bravo.Desktop.Interop;

internal static class ColorModeInterop
{
    /// <summary>
    /// Asks Windows to draw the frame of a window, with its title bar, in the colors of the dark mode or of
    /// the light one.
    /// </summary>
    /// <returns>
    /// <see langword="false"/> when Windows does not do it: the request is documented from Windows 11 on, and
    /// an earlier version keeps the frame as it is.
    /// </returns>
    public static unsafe bool TryUseDarkFrame(nint windowHandle, bool isDark)
    {
        BOOL value = isDark;
        var result = PInvoke.DwmSetWindowAttribute(
            (HWND)windowHandle,
            DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
            &value,
            (uint)sizeof(BOOL));

        return result.Succeeded;
    }

    /// <summary>
    /// Whether a message of a window is the one that Windows sends to every window after a setting of the
    /// system changed, the color mode among them.
    /// </summary>
    public static bool IsSettingChange(int message)
    {
        return message == PInvoke.WM_SETTINGCHANGE;
    }
}
