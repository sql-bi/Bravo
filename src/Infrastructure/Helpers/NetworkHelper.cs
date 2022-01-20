using Sqlbi.Bravo.Infrastructure.Extensions;
using Sqlbi.Bravo.Infrastructure.Windows.Interop;
using Sqlbi.Bravo.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    internal static class NetworkHelper
    {
        public static readonly string LocalHost = "localhost";

        public static bool IsPBICloudDatasetServer(string address)
        {
            if (address.Contains(Uri.SchemeDelimiter) && Uri.TryCreate(address, UriKind.Absolute, out var addressUri))
            {
                var isGenericDataset = addressUri.Scheme.Equals(PBICloudService.PBIDatasetProtocolScheme, StringComparison.OrdinalIgnoreCase);
                var isPremiumDataset = addressUri.Scheme.Equals(PBICloudService.PBIPremiumProtocolScheme, StringComparison.OrdinalIgnoreCase);

                return isPremiumDataset || isGenericDataset;
            }

            return false;
        }

        public static IPAddress GetLoopbackAddress()
        {
            if (Socket.OSSupportsIPv6)
            {
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                var loopbackInterface = networkInterfaces.SingleOrDefault((i) => i.NetworkInterfaceType == NetworkInterfaceType.Loopback);

                if (loopbackInterface?.OperationalStatus == OperationalStatus.Up)
                {
                    if (loopbackInterface.Supports(NetworkInterfaceComponent.IPv6))
                        return IPAddress.IPv6Loopback;
                }
            }

            // Fallback to IPv4
            return IPAddress.Loopback;
        }

        public static Process? FindEndPointProcess(IPEndPoint endpoint)
        {
            switch (endpoint.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    {
                        var ipv4Connections = GetTcpConnections<Iphlpapi.MIB_TCPROW_OWNER_PID, Iphlpapi.MIB_TCPTABLE_OWNER_PID>(AddressFamily.InterNetwork);
                        var endpointConnection = ipv4Connections.Where((c) => c.LocalEndPoint.Equals(endpoint)).DefaultIfEmpty();
                        if (endpointConnection is not null)
                        {
                            var process = ProcessExtensions.SafeGetProcessById(endpointConnection.Single().ProcessId);
                            return process;
                        }
                    }
                    break;
                case AddressFamily.InterNetworkV6:
                    {
                        var ipv6Connections = GetTcpConnections<Iphlpapi.MIB_TCP6ROW_OWNER_PID, Iphlpapi.MIB_TCP6TABLE_OWNER_PID>(AddressFamily.InterNetworkV6);
                        var endpointConnection = ipv6Connections.Where((c) => c.LocalEndPoint.Equals(endpoint)).DefaultIfEmpty();
                        if (endpointConnection is not null)
                        {
                            var process = ProcessExtensions.SafeGetProcessById(endpointConnection.Single().ProcessId);
                            return process;
                        }
                    }
                    break;
            }

            return null;
        }

        public static IEnumerable<(IPEndPoint EndPoint, TcpState State, int ProcessId)> GetTcpConnections(Func<(IPEndPoint EndPoint, TcpState State, int ProcessId), bool> predicate)
        {
            var ipv4Connections = GetTcpConnections<Iphlpapi.MIB_TCPROW_OWNER_PID, Iphlpapi.MIB_TCPTABLE_OWNER_PID>(AddressFamily.InterNetwork).Select((r) => (r.LocalEndPoint, r.TcpState, r.ProcessId));
            foreach (var connection in ipv4Connections.Where(predicate))
                yield return connection;

            var ipv6Connections = GetTcpConnections<Iphlpapi.MIB_TCP6ROW_OWNER_PID, Iphlpapi.MIB_TCP6TABLE_OWNER_PID>(AddressFamily.InterNetworkV6).Select((r) => (r.LocalEndPoint, r.TcpState, r.ProcessId));
            foreach (var connection in ipv6Connections.Where(predicate))
                yield return connection;
        }

        private static TRow[] GetTcpConnections<TRow, TTable>(AddressFamily addressFamily)
        {
            switch (addressFamily)
            {
                case AddressFamily.InterNetwork:
                    if (!Socket.OSSupportsIPv4) return Array.Empty<TRow>();
                    break;
                case AddressFamily.InterNetworkV6:
                    if (!Socket.OSSupportsIPv6) return Array.Empty<TRow>();
                    break;
                default:
                    throw new ArgumentException("Unsupported addressing scheme", paramName: nameof(addressFamily));
            }

            var ipVersion = (uint)addressFamily;
            var dwOutBufLen = 0u;

            var retval = Iphlpapi.GetExtendedTcpTable(pTcpTable: IntPtr.Zero, ref dwOutBufLen, order: false, ipVersion, Iphlpapi.TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL);
            if (retval == NativeMethods.ERROR_INSUFFICIENT_BUFFER)
            {
                var pTcpTable = Marshal.AllocHGlobal((int)dwOutBufLen);
                try
                {
                    retval = Iphlpapi.GetExtendedTcpTable(pTcpTable, ref dwOutBufLen, order: false, ipVersion, Iphlpapi.TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL);
                    if (retval == NativeMethods.NO_ERROR)
                    {
                        var tableObject = Marshal.PtrToStructure(pTcpTable, typeof(TTable));
                        if (tableObject != null)
                        {
                            var table = (TTable)tableObject;
                            var tableNumEntries = (uint)(typeof(TTable).GetField("dwNumEntries")?.GetValue(table) ?? throw new InvalidOperationException("Table field not found [dwNumEntries]"));
                            var rows = new TRow[tableNumEntries];
                            var ptr = (IntPtr)((long)pTcpTable + Marshal.SizeOf(tableNumEntries));

                            for (var i = 0; i < tableNumEntries; i++)
                            {
                                var rowObject = Marshal.PtrToStructure(ptr, typeof(TRow));
                                if (rowObject != null)
                                {
                                    var row = (TRow)rowObject;
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
            }

            if (retval == NativeMethods.ERROR_NO_DATA)
            {
                return Array.Empty<TRow>();
            }

            throw new NetworkInformationException(errorCode: (int)retval);
        }
    }
}
