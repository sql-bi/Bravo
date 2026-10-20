using System;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Host;
using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Bravo.Infrastructure.Messages;
using Sqlbi.Bravo.Infrastructure.SingleInstance;
using Xunit;

namespace Bravo.Tests.Host;

public class BravoSingleInstanceTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(5);

    private static T NotCalled<T>() => throw new InvalidOperationException("The arbitration must not call this factory.");

    [Fact]
    public void ClaimOrRedirect_NoPrimary_ClaimsTheInstanceWithoutSendingAPayload()
    {
        var options = TestInstanceOptions.Create();

        using (var server = BravoSingleInstance.ClaimOrRedirect(options, NotCalled<byte[]>, NotCalled<ILoggerFactory>))
        {
            Assert.NotNull(server);
            Assert.False(SingleInstanceServer.TryStart(options, out _));
        }
    }

    [Fact]
    public void ClaimOrRedirect_PrimaryRunning_DeliversThePayloadAndReturnsNull()
    {
        var options = TestInstanceOptions.Create();
        byte[]? received = null;

        Assert.True(SingleInstanceServer.TryStart(options, out var primary));

        using (primary)
        using (var activated = new ManualResetEventSlim())
        {
            primary.Activated += (sender, e) =>
            {
                received = e.Payload;
                activated.Set();
            };

            Assert.Null(BravoSingleInstance.ClaimOrRedirect(options, () => Encoding.UTF8.GetBytes("hello"), NotCalled<ILoggerFactory>));
            Assert.True(activated.Wait(s_timeout));
        }

        Assert.Equal("hello", Encoding.UTF8.GetString(received!));
    }

    [Fact]
    public void ClaimOrRedirect_PrimaryUnreachable_LogsAnErrorAndReturnsNull()
    {
        var options = TestInstanceOptions.Create() with { ConnectTimeout = TimeSpan.FromMilliseconds(200) };
        var loggerFactory = new FakeLoggerFactory();

        Assert.True(SingleInstanceServer.TryStart(options, out var primary));

        using (primary)
        using (var holder = new NamedPipeClientStream(".", options.PipeName, PipeDirection.Out, PipeOptions.CurrentUserOnly))
        {
            // A client that connects and never writes holds the primary's only pipe instance until the read timeout.
            holder.Connect((int)s_timeout.TotalMilliseconds);

            Assert.Null(BravoSingleInstance.ClaimOrRedirect(options, () => [1], () => loggerFactory));
        }

        var entry = Assert.Single(loggerFactory.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Equal(1, entry.EventId.Id);
        Assert.NotNull(entry.Exception);
        Assert.Contains(nameof(SingleInstanceSendStatus.OwnerUnavailable), entry.Message);
        Assert.Equal(1, loggerFactory.DisposeCount);
    }

    [Fact]
    public void ClaimOrRedirect_DoesNotReferenceAppEnvironment()
    {
        // A secondary runs this path before it exits: the AppEnvironment static constructor probes the WebView2
        // runtime, the deployment mode and the registry, which a process that only forwards its arguments must not do.
        var claimOrRedirect = typeof(BravoSingleInstance).GetMethod(nameof(BravoSingleInstance.ClaimOrRedirect), Type.EmptyTypes);
        Assert.NotNull(claimOrRedirect);

        var referencedTypes = CallGraph.GetReferencedTypes(claimOrRedirect);

        Assert.DoesNotContain(typeof(AppEnvironment), referencedTypes);
    }

    [Fact]
    public void CallGraph_MethodThatReadsAppEnvironment_ReferencesIt()
    {
        var toJsonElement = typeof(AppInstanceStartupMessageExtensions).GetMethod(nameof(AppInstanceStartupMessageExtensions.ToJsonElement));
        Assert.NotNull(toJsonElement);

        var referencedTypes = CallGraph.GetReferencedTypes(toJsonElement);

        Assert.Contains(typeof(AppEnvironment), referencedTypes);
    }
}
