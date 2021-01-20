using System;
using System.Runtime.InteropServices;

namespace Sqlbi.Bravo
{
    // Originally from https://gist.github.com/BoyCook/5075907#file-messagehelper
    public class MessageHelper
    {
        [DllImport("User32.dll", EntryPoint = "FindWindow")]
        public static extern int FindWindow(string lpClassName, string lpWindowName);

        //For use with WM_COPYDATA and COPYDATASTRUCT
        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        public static extern int SendMessage(int hWnd, int Msg, int wParam, ref CopyDataStruct lParam);

        public const int WM_COPYDATA = 0x4A;

        //Used for WM_COPYDATA for string messages
        public struct CopyDataStruct
        {
            public IntPtr dwData;
            public int cbData;
            public IntPtr lpData;
        }

        // Passing multiple strings isn't reliable
        // This may not be the best solution but it works
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct ConnectionInfo
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string Details;
        }

        public int SendConnectionInfoMessage(int hWnd, int wParam, ConnectionInfo connInfo)
        {
            var result = 0;

            if (hWnd > 0)
            {
                var buffer = Marshal.AllocHGlobal(Marshal.SizeOf(connInfo));
                Marshal.StructureToPtr(connInfo, buffer, false);
                Marshal.FreeHGlobal(buffer);

                CopyDataStruct cds;
                cds.dwData = (IntPtr)100;
                cds.lpData = buffer;
                cds.cbData = Marshal.SizeOf(connInfo);
                result = SendMessage(hWnd, WM_COPYDATA, wParam, ref cds);
            }

            return result;
        }

        public int GetWindowId(string windowName) => FindWindow(null, windowName);

        public static (string DbName, string ServerName, string ParentProcName, string ParentWindowTitle) ExtractDetails(ConnectionInfo connInfo)
        {
            var details = connInfo.Details.Split('|');

            return details.Length == 4
                ? (details[0], details[1], details[2], details[3])
                : (string.Empty, string.Empty, string.Empty, string.Empty);
        }

        public static ConnectionInfo CreateConnectionInfo(string databaseName, string serverName, string parentProcName, string parentWindowName)
        {
            return new MessageHelper.ConnectionInfo
            {
                Details = $"{databaseName}|{serverName}|{parentProcName}|{parentWindowName}"
            };
        }
    }
}
