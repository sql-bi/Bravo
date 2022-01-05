using Sqlbi.Bravo.Infrastructure.Windows.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    internal static class NetworkHelper
    {
        public static IPAddress GetLoopbackAddress()
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            var loopbackInterface = networkInterfaces.SingleOrDefault((i) => i.NetworkInterfaceType == NetworkInterfaceType.Loopback);

            if (loopbackInterface?.OperationalStatus == OperationalStatus.Up)
            {
                if (loopbackInterface.Supports(NetworkInterfaceComponent.IPv6))
                    return IPAddress.IPv6Loopback;
            }

            // Fallback to IPv4
            return IPAddress.Loopback;
        }

        public static IEnumerable<(IPEndPoint EndPoint, TcpState State, int ProcessId)> GetTcpConnections(Func<(IPEndPoint EndPoint, TcpState State, int ProcessId), bool> predicate)
        {
            var rows = GetTcpRows();
            var connections = rows.Select((r) => (r.LocalEndPoint, r.TcpState, r.ProcessId)).Where(predicate).ToArray();

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
                    var tableObject = Marshal.PtrToStructure(pTcpTable, typeof(NativeMethods.MIB_TCPTABLE_OWNER_PID));
                    if (tableObject != null)
                    {
                        var table = (NativeMethods.MIB_TCPTABLE_OWNER_PID)tableObject;
                        var rows = new NativeMethods.MIB_TCPROW_OWNER_PID[table.dwNumEntries];
                        var ptr = (IntPtr)((long)pTcpTable + Marshal.SizeOf(table.dwNumEntries));

                        for (var i = 0; i < table.dwNumEntries; i++)
                        {
                            var rowObject = Marshal.PtrToStructure(ptr, typeof(NativeMethods.MIB_TCPROW_OWNER_PID));
                            if (rowObject != null)
                            {
                                var row = (NativeMethods.MIB_TCPROW_OWNER_PID)rowObject;
                                ptr = (IntPtr)((long)ptr + Marshal.SizeOf(row));
                                rows[i] = row;
                            }
                        }

                        return rows;
                    }
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
