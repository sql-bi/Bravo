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
/// Exercises the reporting paths against a real pipe. Each test uses a unique pipe name so they
/// can run in parallel and never claim the production one.
/// </summary>
public class BravoApplicationInstanceTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(5);

    private static SingleInstanceOptions CreateOptions() => new()
    {
        PipeName = $"Bravo.Tests.{Guid.NewGuid():N}",
        ConnectTimeout = TimeSpan.FromSeconds(1),
    };

    [Fact]
    public void RedirectActivationToPrimary_NoPrimary_LogsAnErrorAndTracksTheException()
    {
        var options = CreateOptions();
        var telemetry = new FakeTelemetry();
        var loggerFactory = new FakeLoggerFactory();

        using var instance = new BravoApplicationInstance(options, server: null, telemetry, loggerFactory);

        Assert.False(instance.IsPrimary);

        instance.RedirectActivationToPrimary();

        var exception = Assert.Single(telemetry.Exceptions);
        var entry = Assert.Single(loggerFactory.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Equal(2, entry.EventId.Id);
        Assert.Same(exception, entry.Exception);
        Assert.Contains(nameof(SingleInstanceSendStatus.OwnerUnavailable), entry.Message);
    }

    [Fact]
    public void Activated_ListenerReportsAFailure_LogsAWarningAndTracksTheException()
    {
        var options = CreateOptions();
        var telemetry = new FakeTelemetry();
        var loggerFactory = new FakeLoggerFactory();

        Assert.True(SingleInstanceServer.TryStart(options, out var server));
        using var instance = new BravoApplicationInstance(options, server, telemetry, loggerFactory);

        Assert.True(instance.IsPrimary);

        // Connects and closes without writing: the listener reports an empty payload.
        using (var silentClient = new NamedPipeClientStream(".", options.PipeName, PipeDirection.Out, PipeOptions.CurrentUserOnly))
        {
            silentClient.Connect((int)s_timeout.TotalMilliseconds);
        }

        Assert.True(SpinWait.SpinUntil(() => telemetry.Exceptions.Count > 0, s_timeout));

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
        var options = CreateOptions();
        var telemetry = new FakeTelemetry();
        var loggerFactory = new FakeLoggerFactory();

        Assert.True(SingleInstanceServer.TryStart(options, out var server));
        using var instance = new BravoApplicationInstance(options, server, telemetry, loggerFactory);

        using var activated = new ManualResetEventSlim();
        InstanceActivationRequestedEventArgs? activation = null;
        instance.ActivationRequested += (_, e) =>
        {
            activation = e;
            activated.Set();
        };

        Assert.True(SingleInstanceClient.Send(options, Encoding.UTF8.GetBytes("not json")).IsDelivered);
        Assert.True(activated.Wait(s_timeout));

        Assert.NotNull(activation);
        Assert.Null(activation.StartupMessage);

        var exception = Assert.IsAssignableFrom<JsonException>(Assert.Single(telemetry.Exceptions));
        var entry = Assert.Single(loggerFactory.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal(3, entry.EventId.Id);
        Assert.Same(exception, entry.Exception);
    }
}
