using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace Sqlbi.Bravo.Infrastructure.Windows
{
    internal static class Win32Network
    {
        public static IEnumerable<(IPEndPoint EndPoint, TcpState State, int ProcessId)> GetTcpConnections()
        {
            var rows = GetTcpRows();
            var connections = rows.Select((r) => (r.LocalEndPoint, r.TcpState, r.ProcessId)).ToArray();

            return connections;
        }

        private static NativeMethods.MIB_TCPROW_OWNER_PID[] GetTcpRows()
        {
            const int AF_INET = (int)System.Net.Sockets.AddressFamily.InterNetwork;
            uint dwOutBufLen = 0;

            _ = NativeMethods.GetExtendedTcpTable(IntPtr.Zero, ref dwOutBufLen, sort: false, AF_INET, NativeMethods.TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL);
            var pTcpTable = Marshal.AllocHGlobal((int)dwOutBufLen);

            try
            {
                if (NativeMethods.GetExtendedTcpTable(pTcpTable, ref dwOutBufLen, sort: false, AF_INET, NativeMethods.TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL) == 0)
                {
                    var table = (NativeMethods.MIB_TCPTABLE_OWNER_PID)Marshal.PtrToStructure(pTcpTable, typeof(NativeMethods.MIB_TCPTABLE_OWNER_PID));
                    var rows = new NativeMethods.MIB_TCPROW_OWNER_PID[table.dwNumEntries];
                    var ptr = (IntPtr)((long)pTcpTable + Marshal.SizeOf(table.dwNumEntries));

                    for (var i = 0; i < table.dwNumEntries; i++)
                    {
                        var row = (NativeMethods.MIB_TCPROW_OWNER_PID)Marshal.PtrToStructure(ptr, typeof(NativeMethods.MIB_TCPROW_OWNER_PID));
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
            
            return Array.Empty<NativeMethods.MIB_TCPROW_OWNER_PID>();
        }
    }
}
