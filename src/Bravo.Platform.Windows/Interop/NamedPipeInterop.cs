using Microsoft.Win32.SafeHandles;
using Windows.Win32;

namespace Bravo.Platform.Windows.Interop;

internal static class NamedPipeInterop
{
    /// <summary>
    /// The process at the other end of a pipe that a client has connected.
    /// </summary>
    public static bool TryGetServerProcessId(SafePipeHandle pipe, out int processId)
    {
        var found = PInvoke.GetNamedPipeServerProcessId(pipe, out var serverProcessId);
        processId = (int)serverProcessId;
        return found;
    }
}
