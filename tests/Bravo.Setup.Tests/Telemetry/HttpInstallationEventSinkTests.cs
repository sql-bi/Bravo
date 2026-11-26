using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Bravo.Desktop.Tests.Telemetry;
using Bravo.Setup.Telemetry;
using Xunit;

namespace Bravo.Setup.Tests.Telemetry;

public sealed class HttpInstallationEventSinkTests
{
    private static readonly InstallationEvent s_event = new(
        "Install",
        "e4f87d099028e128ea2b413f4fa6fc741426bef314de588b0920e89f22015bd0",
        "Microsoft Windows NT 10.0.26100.0",
        new InstalledProduct("perMachine", "Bravo", "1.1.0", "1.1.0.0"));

    [Fact]
    public async Task Send_Event_PostsItsItemToTheEndpointBeforeItReturns()
    {
        await using var ingestion = new TestIngestion();
        var sink = CreateSink(ingestion.ConnectionString, TimeSpan.FromSeconds(30));

        sink.Send(s_event);

        var item = Assert.Single(ingestion.Items);
        Assert.Equal("00000000-0000-0000-0000-000000000000", item.GetProperty("iKey").GetString());
        Assert.Equal("Install", item.GetProperty("data").GetProperty("baseData").GetProperty("name").GetString());
        var tags = item.GetProperty("tags");
        Assert.Equal(s_event.UserId, tags.GetProperty("ai.user.id").GetString());
        Assert.True(Guid.TryParse(tags.GetProperty("ai.session.id").GetString(), out _));
    }

    [Fact]
    public async Task Send_TwoEvents_GivesEachASessionOfItsOwn()
    {
        await using var ingestion = new TestIngestion();
        var sink = CreateSink(ingestion.ConnectionString, TimeSpan.FromSeconds(30));

        sink.Send(s_event);
        sink.Send(s_event);

        var items = ingestion.Items;
        Assert.Equal(2, items.Count);
        Assert.NotEqual(
            items[0].GetProperty("tags").GetProperty("ai.session.id").GetString(),
            items[1].GetProperty("tags").GetProperty("ai.session.id").GetString());
    }

    // The connection is accepted, and the request never answered.
    [Fact]
    public async Task Send_EndpointThatNeverAnswers_ReturnsAfterTheTimeout()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var accepting = listener.AcceptTcpClientAsync(TestContext.Current.CancellationToken).AsTask();
        var sink = CreateSink(ConnectionString(listener), TimeSpan.FromSeconds(1));
        var watch = Stopwatch.StartNew();

        sink.Send(s_event);

        Assert.InRange(watch.Elapsed, TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(10));
        Assert.True(accepting.IsCompletedSuccessfully, "The sink did not connect to the endpoint.");
        using var connection = await accepting;
    }

    [Fact]
    public void Send_EndpointThatRefusesTheConnection_ReturnsWithoutFailing()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var connectionString = ConnectionString(listener);
        listener.Stop();
        var sink = CreateSink(connectionString, TimeSpan.FromSeconds(30));

        var exception = Record.Exception(() => sink.Send(s_event));

        Assert.Null(exception);
    }

    // A failure of another kind than the network, as the client raises for a scheme that it does not know.
    [Fact]
    public void Send_RequestThatFailsWithAnotherError_ReturnsWithoutFailing()
    {
        var destination = new TelemetryDestination("00000000-0000-0000-0000-000000000000", new Uri("ftp://127.0.0.1/v2/track"));
        var sink = new HttpInstallationEventSink(destination, TimeProvider.System, TimeSpan.FromSeconds(30));

        var exception = Record.Exception(() => sink.Send(s_event));

        Assert.Null(exception);
    }

    private static HttpInstallationEventSink CreateSink(string connectionString, TimeSpan timeout)
    {
        return new HttpInstallationEventSink(TelemetryDestination.Parse(connectionString), TimeProvider.System, timeout);
    }

    private static string ConnectionString(TcpListener listener)
    {
        return TestIngestion.ConnectionStringFor(((IPEndPoint)listener.LocalEndpoint).Port);
    }
}
