namespace Sqlbi.Bravo.Infrastructure.Windows.Interop;

using System.ComponentModel;
using System.Runtime.InteropServices;

internal static class Shell32
{
    public static string[] CommandLineToArgs(string commandLine)
    {
        var argv = CommandLineToArgvW(commandLine, out var numArgs);
        if (argv == IntPtr.Zero)
            throw new InvalidOperationException($"CommandLineToArgvW failed", new Win32Exception(Marshal.GetLastWin32Error()));

        try
        {
            var args = new string[numArgs];
            for (var i = 0; i < numArgs; i++)
            {
                var ptr = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
                args[i] = Marshal.PtrToStringUni(ptr) ?? string.Empty; // not expected to be null
            }

            return args;
        }
        finally
        {
            _ = Kernel32.LocalFree(argv);
        }
    }

    [DllImport(ExternDll.Shell32, SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CommandLineToArgvW([MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine, out int pNumArgs);
}
