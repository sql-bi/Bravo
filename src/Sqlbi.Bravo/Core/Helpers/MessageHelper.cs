using Sqlbi.Bravo.Core.Settings;
using Sqlbi.Bravo.Core.Windows;
using System;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Core.Helpers
{
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

        public static void TrySendConnectionInfo(string windowName, ConnectionInfo connectionInfo)
        {
            var hWnd = NativeMethods.FindWindow(lpClassName: null, windowName);
            if (hWnd != IntPtr.Zero)
            {
                var json = JsonSerializer.Serialize(connectionInfo);
                var bytes = System.Text.Encoding.Unicode.GetBytes(json);

                NativeMethods.COPYDATASTRUCT copyData;
                copyData.dwData = (IntPtr)100;
                copyData.lpData = json;
                copyData.cbData = bytes.Length + 1;

                _ = NativeMethods.SendMessage(hWnd, NativeMethods.WM_COPYDATA, wParam: 0, ref copyData);
            }
        }

        public static RuntimeSummary TryReceiveConnectionInfo(IntPtr ptr)
        {
            var copyData = (NativeMethods.COPYDATASTRUCT)Marshal.PtrToStructure(ptr, typeof(NativeMethods.COPYDATASTRUCT));
            if (copyData.cbData == 0)
                return null;

            var json = copyData.lpData;
            var connectionInfo = JsonSerializer.Deserialize<ConnectionInfo>(json);

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
    }
}
