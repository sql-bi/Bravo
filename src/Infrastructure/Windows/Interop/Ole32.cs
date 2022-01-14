using System;
using System.Runtime.InteropServices;

namespace Sqlbi.Bravo.Infrastructure.Windows.Interop
{
    internal static class Ole32
    {
        [DllImport("ole32.dll", ExactSpelling = true)]
        public static extern HRESULT RevokeDragDrop(IntPtr hWnd);
    }
}
