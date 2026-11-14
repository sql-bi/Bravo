using System;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;

namespace Bravo.Desktop.Interop;

internal static class ColorModeInterop
{
    // The number that Windows 10 gave the request before its version 2004. No header names it.
    private const DWMWINDOWATTRIBUTE UseImmersiveDarkModeBefore2004 = (DWMWINDOWATTRIBUTE)19;

    /// <summary>
    /// Whether Windows draws the frame of a window again by itself after <see cref="TryUseDarkFrame"/>.
    /// Windows 11 does. Windows 10 goes on drawing the frame it had until the window gains or loses the
    /// activation: there the caller asks for it with <see cref="RedrawFrame"/>.
    /// </summary>
    public static bool RedrawsFrameItself => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000);

    /// <summary>
    /// Asks Windows to draw the frame of a window, with its title bar, in the colors of the dark mode or of
    /// the light one.
    /// </summary>
    /// <returns>
    /// <see langword="false"/> when Windows does not do it. The request is documented from Windows 11 on;
    /// Windows 10 knows it from version 2004, and by another number from version 1809. An earlier version
    /// keeps the frame as it is.
    /// </returns>
    public static unsafe bool TryUseDarkFrame(nint windowHandle, bool isDark)
    {
        var window = (HWND)windowHandle;
        BOOL value = isDark;
        var size = (uint)sizeof(BOOL);

        var result = PInvoke.DwmSetWindowAttribute(window, DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, &value, size);
        if (result.Failed)
        {
            result = PInvoke.DwmSetWindowAttribute(window, UseImmersiveDarkModeBefore2004, &value, size);
        }

        return result.Succeeded;
    }

    /// <summary>
    /// Makes Windows draw the frame of a window again, with what was asked for it. Windows draws the frame
    /// when it tells a window that the activation changed: the window is told the opposite of its state,
    /// and then its state.
    /// </summary>
    /// <param name="windowHandle">The window, which belongs to the calling thread.</param>
    /// <param name="isActive">Whether the window is the active one: its frame is left drawn as such.</param>
    public static void RedrawFrame(nint windowHandle, bool isActive)
    {
        var window = (HWND)windowHandle;
        _ = PInvoke.SendMessage(window, PInvoke.WM_NCACTIVATE, ToParameter(!isActive), default);
        _ = PInvoke.SendMessage(window, PInvoke.WM_NCACTIVATE, ToParameter(isActive), default);
    }

    /// <summary>
    /// Whether a message of a window is the one that Windows sends to every window after a setting of the
    /// system changed, the color mode among them.
    /// </summary>
    public static bool IsSettingChange(int message)
    {
        return message == PInvoke.WM_SETTINGCHANGE;
    }

    private static WPARAM ToParameter(bool value)
    {
        return new WPARAM(value ? 1u : 0u);
    }
}
