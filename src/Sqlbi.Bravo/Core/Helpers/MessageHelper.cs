using Sqlbi.Bravo.Core.Settings;
using Sqlbi.Bravo.Core.Windows;
using System;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Core.Helpers
{
    // Originally from https://gist.github.com/BoyCook/5075907#file-messagehelper
    internal class MessageHelper
    {
        public class ConnectionInfo
        {
            [JsonPropertyName("d")]
            public string DatabaseName { get; set; }

            [JsonPropertyName("s")]
            public string ServerName { get; set; }

            [JsonPropertyName("p")]
            public string ParentProcessName { get; set; }

            [JsonPropertyName("t")]
            public string ParentProcessMainWindowTitle { get; set; }
        }

        // Passing multiple strings isn't reliable
        // This may not be the best solution but it works
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct CONNECTIONDATASTRUCT
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string Data;
        }

        public static void TrySendConnectionInfo(string windowName, ConnectionInfo connectionInfo)
        {
            var hWnd = NativeMethods.FindWindow(lpClassName: null, windowName);
            if (hWnd != IntPtr.Zero)
            {
                CONNECTIONDATASTRUCT connectionData;
                connectionData.Data = JsonSerializer.Serialize(connectionInfo);

                var buffer = Marshal.AllocHGlobal(Marshal.SizeOf(connectionData));
                Marshal.StructureToPtr(connectionData, buffer, false);
                Marshal.FreeHGlobal(buffer);

                NativeMethods.COPYDATASTRUCT copyData;
                copyData.dwData = (IntPtr)100;
                copyData.lpData = buffer;
                copyData.cbData = Marshal.SizeOf(connectionData);

                _ = NativeMethods.SendMessage(hWnd, NativeMethods.WM_COPYDATA, wParam: 0, ref copyData);
            }
        }

        public static RuntimeSummary TryReceiveConnectionInfo(IntPtr ptr)
        {
            var connectionData = new CONNECTIONDATASTRUCT();

            var copyData = (NativeMethods.COPYDATASTRUCT)Marshal.PtrToStructure(ptr, typeof(NativeMethods.COPYDATASTRUCT));
            if (copyData.cbData == Marshal.SizeOf(connectionData))
            {
                connectionData = (CONNECTIONDATASTRUCT)Marshal.PtrToStructure(copyData.lpData, typeof(CONNECTIONDATASTRUCT));

                var connectionInfo = JsonSerializer.Deserialize<ConnectionInfo>(connectionData.Data);

                System.Diagnostics.Debug.WriteLine($"DatabaseName = '{ connectionInfo.DatabaseName }'");
                System.Diagnostics.Debug.WriteLine($"ServerName = '{ connectionInfo.ServerName }'");
                System.Diagnostics.Debug.WriteLine($"ParentProcessName = '{ connectionInfo.ParentProcessName }'");
                System.Diagnostics.Debug.WriteLine($"ParentProcessMainWindowTitle = '{ connectionInfo.ParentProcessMainWindowTitle }'");

                var runtimeSummary = new RuntimeSummary
                {
                    IsExecutedAsExternalTool = true,
                    ServerName = connectionInfo.ServerName,
                    DatabaseName = connectionInfo.DatabaseName,
                    ParentProcessName = connectionInfo.ParentProcessName,
                    ParentProcessMainWindowTitle = connectionInfo.ParentProcessMainWindowTitle,
                };

                return runtimeSummary;
            }

            return null;
        }
    }
}
