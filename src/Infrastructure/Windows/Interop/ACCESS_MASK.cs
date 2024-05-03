namespace Sqlbi.Bravo.Infrastructure.Windows.Interop;

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
internal struct ACCESS_MASK
{
    public uint Value;
}