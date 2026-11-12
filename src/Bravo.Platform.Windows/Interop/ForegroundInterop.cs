using Windows.Win32;

namespace Bravo.Platform.Windows.Interop;

internal static class ForegroundInterop
{
    /// <summary>
    /// Lets a process bring one of its windows to the foreground.
    /// </summary>
    /// <returns><see langword="false"/> when this process could not come to the foreground itself.</returns>
    public static bool TryAllowSetForeground(int processId)
    {
        return PInvoke.AllowSetForegroundWindow((uint)processId);
    }
}
