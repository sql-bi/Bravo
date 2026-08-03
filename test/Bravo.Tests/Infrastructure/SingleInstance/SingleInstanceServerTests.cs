using System;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using Sqlbi.Bravo.Infrastructure.SingleInstance;
using Xunit;

namespace Bravo.Tests.Infrastructure.SingleInstance;

/// <summary>
/// Exercises the real named pipe: the component is a thin layer over an OS primitive, and the
/// behaviour worth protecting — who wins ownership, and whether the listener survives — only exists
/// against the primitive itself. Each test uses a unique pipe name so they can run in parallel.
/// </summary>
public class SingleInstanceServerTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(5);

    private static SingleInstanceOptions CreateOptions() => new()
    {
        PipeName = $"Bravo.Tests.{Guid.NewGuid():N}",
        ConnectTimeout = TimeSpan.FromSeconds(2),
    };

    private static byte[] Payload(string value) => Encoding.UTF8.GetBytes(value);

    [Fact]
    public void TryStart_NameIsFree_TakesOwnership()
    {
        var options = CreateOptions();

        Assert.True(SingleInstanceServer.TryStart(options, out var server));

        using (server)
        {
            Assert.NotNull(server);
        }
    }

    [Fact]
    public void TryStart_NameIsAlreadyOwned_DoesNotTakeOwnership()
    {
        var options = CreateOptions();

        Assert.True(SingleInstanceServer.TryStart(options, out var owner));

        using (owner)
        {
            Assert.False(SingleInstanceServer.TryStart(options, out var second));
            Assert.Null(second);
        }
    }

    /// <summary>
    /// The reason the pipe is the gate instead of a mutex: ownership cannot outlive the ability to
    /// answer. When the owner goes away the name is free again, so the next process starts normally
    /// rather than timing out against an owner that no longer listens.
    /// </summary>
    [Fact]
    public void TryStart_PreviousOwnerIsGone_TakesOwnership()
    {
        var options = CreateOptions();

        Assert.True(SingleInstanceServer.TryStart(options, out var owner));
        owner.Dispose();

        Assert.True(SingleInstanceServer.TryStart(options, out var next));
        next.Dispose();
    }

    [Fact]
    public void Send_NoOwner_ReportsOwnerUnavailable()
    {
        var options = CreateOptions();

        var result = SingleInstanceClient.Send(options, Payload("ignored"));

        Assert.False(result.IsDelivered);
        Assert.Equal(SingleInstanceSendStatus.OwnerUnavailable, result.Status);
        Assert.NotNull(result.Exception);
    }

    [Fact]
    public void Send_OwnerIsListening_DeliversPayloadVerbatim()
    {
        var options = CreateOptions();
        Assert.True(SingleInstanceServer.TryStart(options, out var server));

        using (server)
        {
            using var received = new ManualResetEventSlim();
            byte[]? payload = null;

            server.Activated += (_, e) =>
            {
                payload = e.Payload;
                received.Set();
            };

            Assert.True(SingleInstanceClient.Send(options, Payload("hello")).IsDelivered);

            Assert.True(received.Wait(s_timeout));
            Assert.Equal("hello", Encoding.UTF8.GetString(payload!));
        }
    }

    /// <summary>
    /// Regression: the listener used to be restarted only after the subscribers had returned, so a
    /// second instance could not be served while the owner was busy — and every later instance was
    /// silently lost after its connection timed out.
    /// </summary>
    [Fact]
    public void Activated_SubscriberIsBlocked_KeepsServingNewInstances()
    {
        var options = CreateOptions();
        Assert.True(SingleInstanceServer.TryStart(options, out var server));

        using (server)
        {
            using var firstReceived = new ManualResetEventSlim();
            using var releaseFirst = new ManualResetEventSlim();
            using var secondReceived = new ManualResetEventSlim();
            var count = 0;

            server.Activated += (_, _) =>
            {
                if (Interlocked.Increment(ref count) == 1)
                {
                    firstReceived.Set();
                    releaseFirst.Wait(s_timeout); // stands in for a modal dialog owning the UI thread
                }
                else
                {
                    secondReceived.Set();
                }
            };

            Assert.True(SingleInstanceClient.Send(options, Payload("first")).IsDelivered);
            Assert.True(firstReceived.Wait(s_timeout));

            Assert.True(SingleInstanceClient.Send(options, Payload("second")).IsDelivered);
            Assert.True(secondReceived.Wait(s_timeout));

            releaseFirst.Set();
        }
    }

    /// <summary>
    /// Regression: a client that connects and never writes used to hold the pipe's only server
    /// instance forever — the owner stayed alive but became unreachable to every later instance,
    /// which is indistinguishable in the field from Bravo being broken.
    /// </summary>
    [Fact]
    public void Activated_ClientConnectsButNeverWrites_RecoversAndKeepsListening()
    {
        var options = CreateOptions() with { ReadTimeout = TimeSpan.FromMilliseconds(300) };
        Assert.True(SingleInstanceServer.TryStart(options, out var server));

        using (server)
        {
            using var errorRaised = new ManualResetEventSlim();
            server.Error += (_, _) => errorRaised.Set();

            using (var stalledClient = new NamedPipeClientStream(
                ".", options.PipeName, PipeDirection.Out, PipeOptions.CurrentUserOnly))
            {
                stalledClient.Connect((int)s_timeout.TotalMilliseconds);
                // Connected, but deliberately never writes: stands in for a stuck or hostile client.
                Assert.True(errorRaised.Wait(s_timeout));
            }

            // The stalled connection released the pipe: a real client is served next.
            using var received = new ManualResetEventSlim();
            byte[]? payload = null;

            server.Activated += (_, e) =>
            {
                payload = e.Payload;
                received.Set();
            };

            Assert.True(SingleInstanceClient.Send(options, Payload("hello")).IsDelivered);
            Assert.True(received.Wait(s_timeout));
            Assert.Equal("hello", Encoding.UTF8.GetString(payload!));
        }
    }

    [Fact]
    public void Send_RepeatedNotifications_AreAllDelivered()
    {
        var options = CreateOptions();
        Assert.True(SingleInstanceServer.TryStart(options, out var server));

        using (server)
        {
            using var allReceived = new CountdownEvent(initialCount: 3);
            server.Activated += (_, _) => allReceived.Signal();

            for (var i = 0; i < 3; i++)
            {
                var result = SingleInstanceClient.Send(options, Payload($"message-{i}"));
                Assert.True(result.IsDelivered, $"send #{i} -> {result.Status}: {result.Exception}");
            }

            Assert.True(allReceived.Wait(s_timeout));
        }
    }

    [Fact]
    public void Activated_PayloadExceedsTheLimit_IsRejectedAndTheOwnerKeepsListening()
    {
        var pipeName = $"Bravo.Tests.{Guid.NewGuid():N}";
        var serverOptions = new SingleInstanceOptions { PipeName = pipeName, MaxPayloadBytes = 32 };
        var clientOptions = new SingleInstanceOptions { PipeName = pipeName, ConnectTimeout = TimeSpan.FromSeconds(2) };

        Assert.True(SingleInstanceServer.TryStart(serverOptions, out var server));

        using (server)
        {
            using var received = new ManualResetEventSlim();
            using var rejected = new ManualResetEventSlim();
            byte[]? payload = null;

            server.Activated += (_, e) =>
            {
                payload = e.Payload;
                received.Set();
            };
            // An oversized payload is a reported rejection, not a silent drop: without this the owner
            // would discard it with nothing recorded anywhere.
            server.Error += (_, _) => rejected.Set();

            Assert.True(SingleInstanceClient.Send(clientOptions, Payload(new string('x', 256))).IsDelivered);
            Assert.True(rejected.Wait(s_timeout));
            Assert.False(received.Wait(TimeSpan.FromSeconds(1)));

            Assert.True(SingleInstanceClient.Send(clientOptions, Payload("small")).IsDelivered);
            Assert.True(received.Wait(s_timeout));
            Assert.Equal("small", Encoding.UTF8.GetString(payload!));
        }
    }

    [Fact]
    public void Send_PayloadExceedsTheConfiguredLimit_Throws()
    {
        var options = new SingleInstanceOptions { PipeName = $"Bravo.Tests.{Guid.NewGuid():N}", MaxPayloadBytes = 8 };

        Assert.Throws<ArgumentOutOfRangeException>(() => SingleInstanceClient.Send(options, Payload("far too long")));
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var options = CreateOptions();
        Assert.True(SingleInstanceServer.TryStart(options, out var server));

        server.Dispose();
        server.Dispose();
    }
}
