using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using static Sqlbi.Bravo.Core.NativeMethods;

namespace Sqlbi.Bravo.Core.Windows
{
    internal static class Network
    {
        public static IEnumerable<(IPEndPoint RemoteEndPoint, IPEndPoint LocalEndPoint, TcpState State, int ProcessId)> GetTcpConnections()
        {
            var connections = GetTcpRows().Select((r) => (r.RemoteEndPoint, r.LocalEndPoint, r.TcpState, r.ProcessId));

            return connections.ToArray();
        }

        private static MIB_TCPROW_OWNER_PID[] GetTcpRows()
        {
            const int AF_INET = (int)System.Net.Sockets.AddressFamily.InterNetwork;
            uint dwOutBufLen = 0;

            _ = NativeMethods.GetExtendedTcpTable(IntPtr.Zero, ref dwOutBufLen, sort: false, AF_INET, NativeMethods.TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL);
            var pTcpTable = Marshal.AllocHGlobal((int)dwOutBufLen);

            try
            {
                if (NativeMethods.GetExtendedTcpTable(pTcpTable, ref dwOutBufLen, sort: false, AF_INET, NativeMethods.TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL) == 0)
                {
                    var table = (MIB_TCPTABLE_OWNER_PID)Marshal.PtrToStructure(pTcpTable, typeof(MIB_TCPTABLE_OWNER_PID));
                    var rows = new MIB_TCPROW_OWNER_PID[table.dwNumEntries];
                    var ptr = (IntPtr)((long)pTcpTable + Marshal.SizeOf(table.dwNumEntries));

                    for (var i = 0; i < table.dwNumEntries; i++)
                    {
                        var row = (MIB_TCPROW_OWNER_PID)Marshal.PtrToStructure(ptr, typeof(MIB_TCPROW_OWNER_PID));
                        rows[i] = row;
                        ptr = (IntPtr)((long)ptr + Marshal.SizeOf(row));
                    }

                    return rows;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pTcpTable);
            }
            
            return Array.Empty<MIB_TCPROW_OWNER_PID>();
        }
    }
}
