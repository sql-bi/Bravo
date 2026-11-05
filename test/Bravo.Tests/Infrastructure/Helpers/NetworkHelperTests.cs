using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Sqlbi.Bravo.Infrastructure.Helpers;
using Sqlbi.Bravo.Infrastructure.Windows.Interop;
using Xunit;

namespace Bravo.Tests.Infrastructure.Helpers;

public class NetworkHelperTests
{
    private const int EngineProcessId = 30992;
    private const int DesktopProcessId = 30364;

    // MIB_TCP_STATE values; the affected Windows 11 builds report a listening socket in state 0 or CLOSED
    private const uint StateZero = 0;
    private const uint StateClosed = 1;
    private const uint StateListen = 2;
    private const uint StateEstablished = 5;
    private const uint StateTimeWait = 11;

    [Theory]
    [InlineData(StateZero)]
    [InlineData(StateClosed)]
    public void SelectLoopbackListener_ListenerNotReportedAsListen_ReturnsItsEndPoint(uint state)
    {
        var connections = new[]
        {
            Ipv4Row(EngineProcessId, "127.0.0.1", 57316, "0.0.0.0", 0, state),
            Ipv4Row(DesktopProcessId, "127.0.0.1", 61001, "127.0.0.1", 57316, StateEstablished),
        };

        var endPoint = NetworkHelper.SelectLoopbackListener(connections, EngineProcessId);

        Assert.Equal(new IPEndPoint(IPAddress.Loopback, 57316), endPoint);
    }

    [Fact]
    public void SelectLoopbackListener_Ipv6ListenerReportedInStateZero_ReturnsItsEndPoint()
    {
        var connections = new[] { Ipv6Row(EngineProcessId, IPAddress.IPv6Loopback, 57316, StateZero) };

        var endPoint = NetworkHelper.SelectLoopbackListener(connections, EngineProcessId);

        Assert.Equal(new IPEndPoint(IPAddress.IPv6Loopback, 57316), endPoint);
    }

    [Fact]
    public void SelectLoopbackListener_UnconnectedRowBeforeListenRow_ReturnsTheListenRow()
    {
        var connections = new[]
        {
            Ipv4Row(EngineProcessId, "127.0.0.1", 50000, "0.0.0.0", 0, StateZero),
            Ipv4Row(EngineProcessId, "127.0.0.1", 57316, "0.0.0.0", 0, StateListen),
        };

        var endPoint = NetworkHelper.SelectLoopbackListener(connections, EngineProcessId);

        Assert.Equal(57316, endPoint?.Port);
    }

    [Fact]
    public void SelectLoopbackListener_ListenRow_DoesNotReadTheRowsAfterIt()
    {
        var connections = RowsThenFailure(Ipv4Row(EngineProcessId, "127.0.0.1", 57316, "0.0.0.0", 0, StateListen));

        var endPoint = NetworkHelper.SelectLoopbackListener(connections, EngineProcessId);

        Assert.Equal(57316, endPoint?.Port);
    }

    [Fact]
    public void SelectLoopbackListener_ConnectedRows_ReturnsNull()
    {
        var connections = new[]
        {
            Ipv4Row(EngineProcessId, "127.0.0.1", 57316, "127.0.0.1", 61001, StateEstablished),
            Ipv4Row(EngineProcessId, "127.0.0.1", 57317, "127.0.0.1", 61002, StateZero),
            Ipv4Row(EngineProcessId, "127.0.0.1", 57318, "127.0.0.1", 61003, StateTimeWait),
        };

        var endPoint = NetworkHelper.SelectLoopbackListener(connections, EngineProcessId);

        Assert.Null(endPoint);
    }

    [Fact]
    public void SelectLoopbackListener_UnconnectedRowWithRemotePort_ReturnsNull()
    {
        var connections = new[] { Ipv4Row(EngineProcessId, "127.0.0.1", 57316, "0.0.0.0", 443, StateZero) };

        var endPoint = NetworkHelper.SelectLoopbackListener(connections, EngineProcessId);

        Assert.Null(endPoint);
    }

    [Theory]
    [InlineData("0.0.0.0", StateListen)]
    [InlineData("192.168.1.10", StateListen)]
    [InlineData("192.168.1.10", StateZero)]
    public void SelectLoopbackListener_RowOnNonLoopbackAddress_ReturnsNull(string localAddress, uint state)
    {
        var connections = new[] { Ipv4Row(EngineProcessId, localAddress, 2383, "0.0.0.0", 0, state) };

        var endPoint = NetworkHelper.SelectLoopbackListener(connections, EngineProcessId);

        Assert.Null(endPoint);
    }

    [Fact]
    public void SelectLoopbackListener_ListenerOfAnotherProcess_ReturnsNull()
    {
        var connections = new[] { Ipv4Row(DesktopProcessId, "127.0.0.1", 57316, "0.0.0.0", 0, StateListen) };

        var endPoint = NetworkHelper.SelectLoopbackListener(connections, EngineProcessId);

        Assert.Null(endPoint);
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    public void GetLoopbackListenerEndPoint_ProcessWithOneLoopbackListener_ReturnsItsEndPoint(string address)
    {
        using var listener = new TcpListener(IPAddress.Parse(address), port: 0);
        listener.Start();

        var endPoint = NetworkHelper.GetLoopbackListenerEndPoint(Environment.ProcessId);

        Assert.Equal((IPEndPoint)listener.LocalEndpoint, endPoint);
    }

    [Fact]
    public void GetLoopbackListenerEndPoint_ProcessWithIpv4AndIpv6LoopbackListeners_ReturnsTheIpv4One()
    {
        using var ipv4Listener = new TcpListener(IPAddress.Loopback, port: 0);
        using var ipv6Listener = new TcpListener(IPAddress.IPv6Loopback, port: 0);
        ipv4Listener.Start();
        ipv6Listener.Start();

        var endPoint = NetworkHelper.GetLoopbackListenerEndPoint(Environment.ProcessId);

        Assert.Equal((IPEndPoint)ipv4Listener.LocalEndpoint, endPoint);
    }

    // Built from the native row, so that the decoding of addresses, ports and state is covered too
    private static NetworkHelper.TcpConnection Ipv4Row(int processId, string localAddress, int localPort, string remoteAddress, int remotePort, uint state)
    {
        var row = new Iphlpapi.MIB_TCPROW_OWNER_PID
        {
            state = state,
            localAddress = Ipv4Address(localAddress),
            localPort = NetworkOrderPort(localPort),
            remoteAddress = Ipv4Address(remoteAddress),
            remotePort = NetworkOrderPort(remotePort),
            owningPid = (uint)processId,
        };

        return new NetworkHelper.TcpConnection(row.LocalEndPoint, row.RemoteEndPoint, row.TcpState, row.ProcessId);
    }

    private static NetworkHelper.TcpConnection Ipv6Row(int processId, IPAddress localAddress, int localPort, uint state)
    {
        var row = new Iphlpapi.MIB_TCP6ROW_OWNER_PID
        {
            localAddr = localAddress.GetAddressBytes(),
            localPort = NetworkOrderPort(localPort),
            remoteAddr = IPAddress.IPv6Any.GetAddressBytes(),
            remotePort = NetworkOrderPort(0),
            state = state,
            owningPid = (uint)processId,
        };

        return new NetworkHelper.TcpConnection(row.LocalEndPoint, row.RemoteEndPoint, row.TcpState, row.ProcessId);
    }

    // Stands for the IPv6 table, which must not be read once an IPv4 LISTEN row is found
    private static IEnumerable<NetworkHelper.TcpConnection> RowsThenFailure(params NetworkHelper.TcpConnection[] rows)
    {
        foreach (var row in rows)
        {
            yield return row;
        }

        throw new InvalidOperationException("The rows after a LISTEN row were read.");
    }

    private static uint Ipv4Address(string address) => BitConverter.ToUInt32(IPAddress.Parse(address).GetAddressBytes(), 0);

    private static byte[] NetworkOrderPort(int port) => new byte[] { (byte)(port >> 8), (byte)port, 0, 0 };
}
