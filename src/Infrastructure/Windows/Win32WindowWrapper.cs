using System;
using System.Windows.Forms;

namespace Sqlbi.Bravo.Infrastructure.Windows
{
    internal class Win32WindowWrapper : IWin32Window
    {
        private Win32WindowWrapper(IntPtr handle)
        {
            Handle = handle;
        }

        public IntPtr Handle { get; private set; }

        public static Win32WindowWrapper CreateFrom(IntPtr handle) => new(handle);
    }
}
