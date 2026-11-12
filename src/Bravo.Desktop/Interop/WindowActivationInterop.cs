using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Bravo.Desktop.Interop;

internal static class WindowActivationInterop
{
    /// <summary>
    /// Restores a minimized window to the size it had and asks Windows to bring it to the foreground.
    /// </summary>
    /// <returns>
    /// <see langword="false"/> when Windows keeps another window in the foreground: it then flashes the
    /// taskbar button of this one.
    /// </returns>
    public static bool RestoreAndActivate(nint windowHandle)
    {
        var window = (HWND)windowHandle;
        if (PInvoke.IsIconic(window))
        {
            _ = PInvoke.ShowWindow(window, SHOW_WINDOW_CMD.SW_RESTORE);
        }

        return PInvoke.SetForegroundWindow(window);
    }
}
