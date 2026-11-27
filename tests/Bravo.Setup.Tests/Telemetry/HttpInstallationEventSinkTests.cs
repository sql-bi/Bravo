using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Bravo.Setup.Telemetry;
using Xunit;

namespace Bravo.Setup.Tests.Telemetry
{
    public sealed class HttpInstallationEventSinkTests
    {
        private static readonly InstallationEvent s_event = new InstallationEvent(
            "Install",
            "e4f87d099028e128ea2b413f4fa6fc741426bef314de588b0920e89f22015bd0",
            "Microsoft Windows NT 10.0.26100.0",
            new InstalledProduct("perMachine", "Bravo", "1.1.0", "1.1.0.0"));

        [Fact]
        public void Send_Event_PostsItsItemToTheEndpointBeforeItReturns()
        {
            using (var ingestion = new TestIngestion())
            {
                var sink = CreateSink(ingestion.ConnectionString, TimeSpan.FromSeconds(30));

                sink.Send(s_event);

                var item = Assert.Single(ingestion.Items);
                Assert.Equal("00000000-0000-0000-0000-000000000000", JsonTree.ReadString(item, "iKey"));
                Assert.Equal("Install", JsonTree.ReadString(item, "data", "baseData", "name"));
                Assert.Equal(s_event.UserId, JsonTree.ReadString(item, "tags", "ai.user.id"));
                Assert.True(Guid.TryParse(JsonTree.ReadString(item, "tags", "ai.session.id"), out _));
                Assert.True(DateTimeOffset.TryParse(JsonTree.ReadString(item, "time"), out _));
            }
        }

        [Fact]
        public void Send_TwoEvents_GivesEachASessionOfItsOwn()
        {
            using (var ingestion = new TestIngestion())
            {
                var sink = CreateSink(ingestion.ConnectionString, TimeSpan.FromSeconds(30));

                sink.Send(s_event);
                sink.Send(s_event);

                var items = ingestion.Items;
                Assert.Equal(2, items.Count);
                Assert.NotEqual(
                    JsonTree.ReadString(items[0], "tags", "ai.session.id"),
                    JsonTree.ReadString(items[1], "tags", "ai.session.id"));
            }
        }

        // The connection is accepted, and the request never answered.
        [Fact]
        public async Task Send_EndpointThatNeverAnswers_ReturnsAfterTheTimeout()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                var accepting = listener.AcceptTcpClientAsync();
                var sink = CreateSink(ConnectionString(listener), TimeSpan.FromSeconds(1));
                var watch = Stopwatch.StartNew();

                sink.Send(s_event);

                Assert.InRange(watch.Elapsed, TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(10));
                Assert.True(accepting.Status == TaskStatus.RanToCompletion, "The sink did not connect to the endpoint.");
                using (await accepting)
                {
                }
            }
            finally
            {
                listener.Stop();
            }
        }

        [Fact]
        public void Send_EndpointThatRefusesTheConnection_ReturnsWithoutFailing()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var connectionString = ConnectionString(listener);
            listener.Stop();
            var sink = CreateSink(connectionString, TimeSpan.FromSeconds(30));

            var exception = Record.Exception(() => sink.Send(s_event));

            Assert.Null(exception);
        }

        // A failure of another kind than the network, as the request of another scheme than HTTP raises.
        [Fact]
        public void Send_RequestThatFailsWithAnotherError_ReturnsWithoutFailing()
        {
            var destination = new TelemetryDestination("00000000-0000-0000-0000-000000000000", new Uri("ftp://127.0.0.1/v2/track"));
            var sink = new HttpInstallationEventSink(destination, TimeSpan.FromSeconds(30));

            var exception = Record.Exception(() => sink.Send(s_event));

            Assert.Null(exception);
        }

        private static HttpInstallationEventSink CreateSink(string connectionString, TimeSpan timeout)
        {
            return new HttpInstallationEventSink(TelemetryDestination.Parse(connectionString), timeout);
        }

        private static string ConnectionString(TcpListener listener)
        {
            return TestIngestion.ConnectionStringFor(((IPEndPoint)listener.LocalEndpoint).Port);
        }
    }
}
