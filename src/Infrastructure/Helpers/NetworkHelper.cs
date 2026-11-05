using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Sqlbi.Bravo.Infrastructure.Extensions;
using Sqlbi.Bravo.Infrastructure.PowerBI.Cloud;
using Sqlbi.Bravo.Infrastructure.Windows.Interop;

namespace Sqlbi.Bravo.Infrastructure.Helpers;

internal static class NetworkHelper
{
    /// <summary>
    /// Standard host name given to the address of the loopback network interface
    /// </summary>
    public static readonly string Localhost = "localhost";

    /// <summary>
    /// A special proxy bypass rule which has the effect of subtracting the implicit loopback rules
    /// https://chromium.googlesource.com/chromium/src/+/HEAD/net/docs/proxy.md#overriding-the-implicit-bypass-rules
    /// </summary>
    public static readonly string LoopbackProxyBypassRule = "<-loopback>";

    /// <summary>
    /// Returns true if the protocol schema for the provided <paramref name="address"/> URI is <see cref="CloudApiClient.ASAzureProtocolScheme"/>
    /// </summary>
    public static bool IsASAzureServer(string address)
    {
        if (address.Contains(Uri.SchemeDelimiter) && Uri.TryCreate(address, UriKind.Absolute, out var addressUri))
        {
            if (addressUri.Scheme.EqualsI(CloudApiClient.ASAzureProtocolScheme))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns true if the protocol schema for the provided <paramref name="address"/> URI is <see cref="CloudApiClient.PBIDatasetProtocolScheme"/> or <see cref="CloudApiClient.PBIPremiumXmlaEndpointProtocolScheme"/>
    /// </summary>
    public static bool IsPBICloudDatasetServer(string address)
    {
        if (address.Contains(Uri.SchemeDelimiter) && Uri.TryCreate(address, UriKind.Absolute, out var addressUri))
        {
            var isGenericDataset = addressUri.Scheme.EqualsI(CloudApiClient.PBIDatasetProtocolScheme);
            var isPremiumDataset = addressUri.Scheme.EqualsI(CloudApiClient.PBIPremiumXmlaEndpointProtocolScheme); // <-- can be removed ??

            return isPremiumDataset || isGenericDataset;
        }

        return false;
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
                    var process = ProcessHelper.SafeGetProcessById(endpointConnection.Single().ProcessId);
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
                    var process = ProcessHelper.SafeGetProcessById(endpointConnection.Single().ProcessId);
                    return process;
                }
            }
            break;
        }

        return null;
    }

    /// <summary>
    /// Returns the loopback endpoint on which the process listens for TCP connections, or <see langword="null"/> if there is none
    /// </summary>
    public static IPEndPoint? GetLoopbackListenerEndPoint(int processId)
    {
        return SelectLoopbackListener(GetTcpConnections(), processId);
    }

    internal static IPEndPoint? SelectLoopbackListener(IEnumerable<TcpConnection> connections, int processId)
    {
        // The first LISTEN row wins and ends the search; without one, the first unconnected row in table order is taken,
        // because some Windows builds do not report LISTEN for a listening socket (see IsUnconnected)
        IPEndPoint? firstUnconnectedEndPoint = null;

        foreach (var connection in connections)
        {
            if (connection.ProcessId != processId || !IPAddress.IsLoopback(connection.LocalEndPoint.Address))
            {
                continue;
            }

            if (connection.State == TcpState.Listen)
            {
                return connection.LocalEndPoint;
            }

            if (IsUnconnected(connection.RemoteEndPoint))
            {
                firstUnconnectedEndPoint ??= connection.LocalEndPoint;
            }
        }

        return firstUnconnectedEndPoint;

        // Windows 11 with KB5127936 (builds 26200.9818 and 26300.9818) lists a listening socket in the TCP table with
        // state 0 or CLOSED instead of LISTEN, and in the TCP_TABLE_OWNER_PID_LISTENER table with FIN_WAIT_1, although
        // it still accepts connections; netstat leaves the row out and Get-NetTCPConnection shows an empty State. The
        // remote endpoint is still the unspecified address with port 0, which a connected socket never has, so it
        // identifies a listener whatever the state. Microsoft has not acknowledged the regression yet:
        // - https://github.com/dotnet/runtime/issues/136672, reproduced by the .NET team below the managed API
        // - https://github.com/DaxStudio/DaxStudio/pull/1507, the same fix in DAX Studio
        static bool IsUnconnected(IPEndPoint remoteEndPoint)
        {
            var isAnyAddress = remoteEndPoint.Address.Equals(IPAddress.Any) || remoteEndPoint.Address.Equals(IPAddress.IPv6Any);
            return isAnyAddress && remoteEndPoint.Port == 0;
        }
    }

    // Lazy, so that the IPv6 table is read only when the IPv4 table has no LISTEN row for the process
    private static IEnumerable<TcpConnection> GetTcpConnections()
    {
        var ipv4Connections = GetTcpConnections<Iphlpapi.MIB_TCPROW_OWNER_PID, Iphlpapi.MIB_TCPTABLE_OWNER_PID>(AddressFamily.InterNetwork);
        foreach (var row in ipv4Connections)
        {
            yield return new TcpConnection(row.LocalEndPoint, row.RemoteEndPoint, row.TcpState, row.ProcessId);
        }

        var ipv6Connections = GetTcpConnections<Iphlpapi.MIB_TCP6ROW_OWNER_PID, Iphlpapi.MIB_TCP6TABLE_OWNER_PID>(AddressFamily.InterNetworkV6);
        foreach (var row in ipv6Connections)
        {
            yield return new TcpConnection(row.LocalEndPoint, row.RemoteEndPoint, row.TcpState, row.ProcessId);
        }
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
                if (retval == NativeMethods.ERROR_SUCCESS)
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

        if (retval == NativeMethods.ERROR_INSUFFICIENT_BUFFER)
        {
            return Array.Empty<TRow>();
        }

        throw new NetworkInformationException(errorCode: (int)retval);
    }

    internal readonly record struct TcpConnection(IPEndPoint LocalEndPoint, IPEndPoint RemoteEndPoint, TcpState State, int ProcessId);
}
