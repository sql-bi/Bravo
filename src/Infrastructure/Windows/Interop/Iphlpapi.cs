using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Sqlbi.Bravo.Infrastructure.Windows.Interop
{
    internal static class Iphlpapi
    {
        public const uint AF_INET = (uint)AddressFamily.InterNetwork;
        public const uint AF_INET6 = (uint)AddressFamily.InterNetworkV6;

        public enum TCP_TABLE_CLASS
        {
            TCP_TABLE_BASIC_LISTENER,
            TCP_TABLE_BASIC_CONNECTIONS,
            TCP_TABLE_BASIC_ALL,
            TCP_TABLE_OWNER_PID_LISTENER,
            TCP_TABLE_OWNER_PID_CONNECTIONS,
            TCP_TABLE_OWNER_PID_ALL,
            TCP_TABLE_OWNER_MODULE_LISTENER,
            TCP_TABLE_OWNER_MODULE_CONNECTIONS,
            TCP_TABLE_OWNER_MODULE_ALL
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_TCPTABLE_OWNER_PID
        {
            public uint dwNumEntries;
            MIB_TCPROW_OWNER_PID table;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_TCPROW_OWNER_PID
        {
            public uint state;
            public uint localAddress;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] localPort;
            public uint remoteAddress;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] remotePort;
            public uint owningPid;

            public IPEndPoint LocalEndPoint => new(localAddress, port: BitConverter.ToUInt16(new byte[2] { localPort[1], localPort[0] }, 0));

            public IPEndPoint RemoteEndPoint => new(remoteAddress, port: BitConverter.ToUInt16(new byte[2] { remotePort[1], remotePort[0] }, 0));

            public TcpState TcpState => (state > 0 && state < 13) ? (TcpState)state : TcpState.Unknown;

            public int ProcessId => (int)owningPid;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_TCP6TABLE_OWNER_PID
        {
            public uint dwNumEntries;
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct, SizeConst = 1)]
            public MIB_TCP6ROW_OWNER_PID[] table;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_TCP6ROW_OWNER_PID
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] localAddr;
            public uint localScopeId;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] localPort;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] remoteAddr;
            public uint remoteScopeId;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] remotePort;
            public uint state;
            public uint owningPid;

            public IPEndPoint LocalEndPoint => new(address: new IPAddress(localAddr, localScopeId), port: BitConverter.ToUInt16(localPort.Take(2).Reverse().ToArray(), 0));

            public IPEndPoint RemoteEndPoint => new(address: new IPAddress(remoteAddr, remoteScopeId), port: BitConverter.ToUInt16(remotePort.Take(2).Reverse().ToArray(), 0));

            public TcpState TcpState => (state > 0 && state < 13) ? (TcpState)state : TcpState.Unknown;

            public int ProcessId => (int)owningPid;
        }

        [DllImport(ExternDll.Iphlpapi)]
        public static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref uint dwOutBufLen, bool order, uint IPVersion, TCP_TABLE_CLASS tableClass, uint reserved = 0u);
    }
}
