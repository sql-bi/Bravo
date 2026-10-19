using System.ComponentModel;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;

namespace Sqlbi.Bravo.Infrastructure.Windows.Interop;

internal static class PipeInterop
{
    /// <summary>
    /// Disconnects the server end of a named pipe instance from its client.
    /// </summary>
    /// <exception cref="Win32Exception">The pipe instance could not be disconnected.</exception>
    public static void DisconnectNamedPipe(SafePipeHandle pipeHandle)
    {
        if (!PInvoke.DisconnectNamedPipe(pipeHandle))
            throw new Win32Exception();
    }
}
