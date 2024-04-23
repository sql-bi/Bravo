namespace Sqlbi.Bravo.Infrastructure.Windows;

internal class Win32WindowWrapper : IWin32Window
{
    public Win32WindowWrapper(IntPtr handle)
    {
        Handle = handle;
    }

    public IntPtr Handle { get; }
}
