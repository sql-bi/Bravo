namespace Sqlbi.Bravo.Infrastructure.Windows.Interop;

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
internal struct GENERIC_MAPPING
{
    public ACCESS_MASK GenericRead;
    public ACCESS_MASK GenericWrite;
    public ACCESS_MASK GenericExecute;
    public ACCESS_MASK GenericAll;
}
