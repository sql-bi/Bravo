using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Host;
using Sqlbi.Bravo.Infrastructure.SingleInstance;
using Xunit;

namespace Bravo.Tests.Host;

/// <summary>
/// Exercises the reporting paths against a real pipe.
/// </summary>
public class BravoApplicationInstanceTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(5);

    [Fact]
    public void Activated_ListenerReportsAFailure_LogsAWarningAndTracksTheException()
    {
        var options = TestInstanceOptions.Create();
        var telemetry = new FakeTelemetry();
        var loggerFactory = new FakeLoggerFactory();

        Assert.True(SingleInstanceServer.TryStart(options, out var server));

        using (server)
        using (new BravoApplicationInstance(server, telemetry, loggerFactory))
        {
            // Connects and closes without writing: the listener reports an empty payload.
            using (var silentClient = new NamedPipeClientStream(".", options.PipeName, PipeDirection.Out, PipeOptions.CurrentUserOnly))
            {
                silentClient.Connect((int)s_timeout.TotalMilliseconds);
            }

            Assert.True(SpinWait.SpinUntil(() => telemetry.Exceptions.Count > 0, s_timeout));
        }

        // Empty payload when the owner accepted before the close; otherwise the accept itself
        // fails with ERROR_NO_DATA.
        var exception = Assert.Single(telemetry.Exceptions);
        Assert.True(exception is InvalidDataException or IOException, exception.ToString());
        var entry = Assert.Single(loggerFactory.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal(1, entry.EventId.Id);
        Assert.Same(exception, entry.Exception);
    }

    [Fact]
    public void Activated_UnreadablePayload_ActivatesWithoutADocumentAndReports()
    {
        var options = TestInstanceOptions.Create();
        var telemetry = new FakeTelemetry();
        var loggerFactory = new FakeLoggerFactory();
        InstanceActivationRequestedEventArgs? activation = null;

        Assert.True(SingleInstanceServer.TryStart(options, out var server));

        using (server)
        using (var instance = new BravoApplicationInstance(server, telemetry, loggerFactory))
        using (var activated = new ManualResetEventSlim())
        {
            instance.ActivationRequested += (sender, e) =>
            {
                activation = e;
                activated.Set();
            };

            Assert.True(SingleInstanceClient.Send(options, Encoding.UTF8.GetBytes("not json")).IsDelivered);
            Assert.True(activated.Wait(s_timeout));
        }

        Assert.NotNull(activation);
        Assert.Null(activation.StartupMessage);

        var exception = Assert.IsAssignableFrom<JsonException>(Assert.Single(telemetry.Exceptions));
        var entry = Assert.Single(loggerFactory.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal(3, entry.EventId.Id);
        Assert.Same(exception, entry.Exception);
    }

    [Fact]
    public void Dispose_StopsTheActivationsAndKeepsThePipe()
    {
        var options = TestInstanceOptions.Create();
        var activations = 0;

        Assert.True(SingleInstanceServer.TryStart(options, out var server));

        using (server)
        using (var dispatched = new ManualResetEventSlim())
        {
            var instance = new BravoApplicationInstance(server, new FakeTelemetry(), new FakeLoggerFactory());
            instance.ActivationRequested += (sender, e) => Interlocked.Increment(ref activations);
            instance.Dispose();

            Assert.False(SingleInstanceServer.TryStart(options, out _));

            // The server invokes all its handlers in one call: when the probe runs, a handler left by the instance would have run.
            server.Activated += (sender, e) => dispatched.Set();

            Assert.True(SingleInstanceClient.Send(options, Encoding.UTF8.GetBytes("{}")).IsDelivered);
            Assert.True(dispatched.Wait(s_timeout));
        }

        Assert.Equal(0, Volatile.Read(ref activations));
    }
}
