using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Api.Tests;
using Bravo.Core.Policies;
using Bravo.Core.Settings;
using Bravo.Core.Updates;
using Bravo.Desktop.Diagnostics;
using Bravo.Desktop.Telemetry;
using Bravo.Platform.Windows.Tests;
using Bravo.Platform.Windows.Tests.Network;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Bravo.Desktop.Tests.Telemetry;

/// <summary>
/// The telemetry of the host, with the SDK of Application Insights, against an endpoint of the test on the
/// loopback address.
/// </summary>
public sealed class HostExceptionTelemetryTests : IDisposable
{
    // Names that only these tests use: what the item carries can be searched for them.
    private static readonly PersonalNames s_names = new(@"C:\Users\canary-user", "canary-user", "CANARY-PC");

    private readonly TemporaryFolder _folder = new();
    private readonly InMemoryUserSettingsStore _settings = new();
    private readonly RecordingLogger<HostExceptionTelemetry> _logger = new();
    private readonly TelemetryContext _context = TelemetryContext.Create(
        BuildIdentity.Development("1.2.3-test"),
        new TelemetrySession("session-of-the-test"),
        Installation.PerUser,
        "131.0.2903.70");

    private string StorageFolder => _folder.Combine("Telemetry");

    public void Dispose()
    {
        _folder.Dispose();
    }

    [Fact]
    public async Task Track_WithConsent_SendsTheExceptionWithWhatEveryItemOfTheRunCarries()
    {
        await using var ingestion = new TestIngestion();
        using var telemetry = await StartAsync(ingestion.ConnectionString);

        telemetry.Track(Thrown(new InvalidOperationException("Something failed.")));

        var item = Assert.Single(await ingestion.WaitForItemsAsync(1));
        var tags = item.GetProperty("tags");
        Assert.Equal(_context.UserId, tags.GetProperty("ai.user.id").GetString());
        Assert.Equal("session-of-the-test", tags.GetProperty("ai.session.id").GetString());
        Assert.Equal("1.2.3-test", tags.GetProperty("ai.application.ver").GetString());
        Assert.Equal(Environment.OSVersion.ToString(), tags.GetProperty("ai.device.osVersion").GetString());
        var data = item.GetProperty("data").GetProperty("baseData");
        Assert.Equal("PerUser", data.GetProperty("properties").GetProperty("InstallScope").GetString());
        var exception = Assert.Single(data.GetProperty("exceptions").EnumerateArray());
        Assert.Equal("System.InvalidOperationException", exception.GetProperty("typeName").GetString());
        Assert.Equal("Something failed.", exception.GetProperty("message").GetString());
        Assert.Equal(nameof(Thrown), Method(exception.GetProperty("parsedStack")[0]).Split('.')[^1]);
    }

    // The computer is named by a hash in both fields, or the SDK sends its name in the one left empty.
    [Fact]
    public async Task Track_AnyException_NamesTheComputerByItsHashOnly()
    {
        await using var ingestion = new TestIngestion();
        using var telemetry = await StartAsync(ingestion.ConnectionString);

        telemetry.Track(new InvalidOperationException("Something failed."));

        var tags = Assert.Single(await ingestion.WaitForItemsAsync(1)).GetProperty("tags");
        var hash = TelemetryContext.Hash(Environment.MachineName);
        Assert.Equal(hash, tags.GetProperty("ai.cloud.roleInstance").GetString());
        Assert.Equal(hash, tags.GetProperty("ai.internal.nodeName").GetString());
        Assert.DoesNotContain(Environment.MachineName, Assert.Single(ingestion.Bodies), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Track_PersonalAndSensitiveTexts_NeverLeaveTheComputer()
    {
        await using var ingestion = new TestIngestion();
        using var telemetry = await StartAsync(ingestion.ConnectionString);
        const string Message = @"Login failed for <pii>secret-canary</pii> as canary-user on CANARY-PC in C:\Users\canary-user\model.pbix.";

        telemetry.Track(new InvalidOperationException("Outer.", new IOException(Message)));

        await ingestion.WaitForItemsAsync(1);
        var body = Assert.Single(ingestion.Bodies);
        Assert.DoesNotContain("secret-canary", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("canary-user", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CANARY-PC", body, StringComparison.OrdinalIgnoreCase);
    }

    // The item and the records of the log of the same work share its trace: support finds one from the other.
    [Fact]
    public async Task Track_UnderAnActivity_SendsItsTraceAsTheOperation()
    {
        await using var ingestion = new TestIngestion();
        using var telemetry = await StartAsync(ingestion.ConnectionString);
        using var activity = new Activity("Bravo.Tests").SetIdFormat(ActivityIdFormat.W3C).Start();

        telemetry.Track(new InvalidOperationException("Something failed."));

        var tags = Assert.Single(await ingestion.WaitForItemsAsync(1)).GetProperty("tags");
        Assert.Equal(HostLogger.GetTraceId(activity), tags.GetProperty("ai.operation.id").GetString());
    }

    [Fact]
    public async Task StartAsync_BuildWithoutADestination_SendsNothingAndSaysWhy()
    {
        using var telemetry = await StartAsync(connectionString: null);

        telemetry.Track(new InvalidOperationException("Something failed."));

        var entry = Assert.Single(_logger.Entries);
        Assert.Equal((LogLevel.Information, 3), (entry.Level, entry.Id.Id));
        Assert.False(Directory.Exists(StorageFolder));
    }

    [Fact]
    public async Task StartAsync_PolicyThatForbidsIt_SendsNothingAndDeletesWhatWaits()
    {
        await using var ingestion = new TestIngestion();
        KeepAFileInTheStore();
        var telemetry = await StartAsync(ingestion.ConnectionString, new ApplicationPolicies { TelemetryEnabled = false });

        telemetry.Track(new InvalidOperationException("Something failed."));
        await telemetry.StopAsync(TestContext.Current.CancellationToken);
        telemetry.Dispose();

        Assert.Empty(ingestion.Bodies);
        Assert.False(Directory.Exists(StorageFolder));
        Assert.Equal(2, Assert.Single(_logger.Entries).Id.Id);
    }

    [Fact]
    public async Task StartAsync_PolicyThatAllowsIt_AppliesOverTheOptionOfTheUser()
    {
        await using var ingestion = new TestIngestion();
        await TurnTelemetryAsync(on: false);
        using var telemetry = await StartAsync(ingestion.ConnectionString, new ApplicationPolicies { TelemetryEnabled = true });

        telemetry.Track(new InvalidOperationException("Something failed."));

        Assert.Single(await ingestion.WaitForItemsAsync(1));
    }

    [Fact]
    public async Task StartAsync_OptionTurnedOff_SendsNothingAndDeletesWhatWaits()
    {
        await using var ingestion = new TestIngestion();
        await TurnTelemetryAsync(on: false);
        KeepAFileInTheStore();
        var telemetry = await StartAsync(ingestion.ConnectionString);

        telemetry.Track(new InvalidOperationException("Something failed."));
        await telemetry.StopAsync(TestContext.Current.CancellationToken);
        telemetry.Dispose();

        Assert.Empty(ingestion.Bodies);
        Assert.False(Directory.Exists(StorageFolder));
    }

    [Fact]
    public async Task OptionTurnedOffWhileRunning_StopsAtOnceAndDeletesWhatWaits()
    {
        await using var ingestion = new TestIngestion();
        var telemetry = await StartAsync(ingestion.ConnectionString);
        KeepAFileInTheStore();

        await TurnTelemetryAsync(on: false);
        telemetry.Track(new InvalidOperationException("Sent nowhere."));
        await telemetry.StopAsync(TestContext.Current.CancellationToken);
        telemetry.Dispose();

        Assert.Empty(ingestion.Bodies);
        Assert.False(Directory.Exists(StorageFolder));
        Assert.Equal([1, 2], _logger.Entries.Select(entry => entry.Id.Id));
    }

    [Fact]
    public async Task OptionTurnedOnWhileRunning_SendsFromThen()
    {
        await using var ingestion = new TestIngestion();
        await TurnTelemetryAsync(on: false);
        using var telemetry = await StartAsync(ingestion.ConnectionString);

        telemetry.Track(new InvalidOperationException("Sent nowhere."));
        await TurnTelemetryAsync(on: true);
        telemetry.Track(new InvalidOperationException("Sent."));

        var item = Assert.Single(await ingestion.WaitForItemsAsync(1));
        Assert.Equal("Sent.", Message(item));
    }

    [Fact]
    public async Task OtherSettingChanged_LeavesTelemetryAsItWas()
    {
        await using var ingestion = new TestIngestion();
        using var telemetry = await StartAsync(ingestion.ConnectionString);

        await _settings.ReplaceAsync(_settings.Current with { Theme = Theme.Dark }, TestContext.Current.CancellationToken);

        Assert.Equal(1, Assert.Single(_logger.Entries).Id.Id);
    }

    // On .NET the channel keeps nothing when the process ends: the stop of the host keeps what it could not send.
    [Fact]
    public async Task StopAsync_EndpointThatCannotBeReached_KeepsTheExceptionForTheNextStart()
    {
        var port = TestProxy.ClosedAddress().Port;
        var connectionString = TestIngestion.ConnectionStringFor(port);
        var first = await StartAsync(connectionString);
        first.Track(new InvalidOperationException("Kept."));
        await first.StopAsync(TestContext.Current.CancellationToken);
        first.Dispose();
        Assert.NotEmpty(Directory.GetFiles(StorageFolder));

        await using var ingestion = new TestIngestion(port);
        using var second = await StartAsync(connectionString);

        Assert.Equal("Kept.", Message(Assert.Single(await ingestion.WaitForItemsAsync(1))));
    }

    // A host whose start failed is disposed without a stop: what the channel could not send is kept all the same.
    [Fact]
    public async Task Dispose_WithoutAStop_KeepsTheExceptionForTheNextStart()
    {
        var first = await StartAsync(TestIngestion.ConnectionStringFor(TestProxy.ClosedAddress().Port));
        first.Track(new InvalidOperationException("Kept."));

        first.Dispose();

        Assert.NotEmpty(Directory.GetFiles(StorageFolder));
    }

    // A change that was already running when the host stopped leaves the pipeline to the flush of the stop.
    [Fact]
    public async Task OptionTurnedOffWhileStopping_LeavesThePipelineToTheFlush()
    {
        await using var ingestion = new TestIngestion();
        var flushing = new TaskCompletionSource<bool>();
        var pipeline = Substitute.For<ITelemetryPipeline>();
        pipeline.FlushAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(flushing.Task);
        using var telemetry = await StartAsync(ingestion.ConnectionString, pipeline: pipeline, settings: new LateSettingsStore(_settings));

        var stopping = telemetry.StopAsync(TestContext.Current.CancellationToken);
        await TurnTelemetryAsync(on: false);
        flushing.SetResult(true);
        await stopping;

        pipeline.DidNotReceive().Dispose();
    }

    // The SDK, unless the test gives a pipeline of its own.
    private async Task<HostExceptionTelemetry> StartAsync(
        string? connectionString,
        ApplicationPolicies? policies = null,
        ITelemetryPipeline? pipeline = null,
        IUserSettingsStore? settings = null)
    {
        var options = new TelemetryOptions { ConnectionString = connectionString, StorageFolder = StorageFolder };
        var telemetry = new HostExceptionTelemetry(
            options,
            () => pipeline ?? TelemetryPipeline.Create(connectionString!, StorageFolder),
            _context,
            policies ?? ApplicationPolicies.None,
            settings ?? _settings,
            () => s_names,
            _logger);
        await telemetry.StartAsync(TestContext.Current.CancellationToken);

        return telemetry;
    }

    private Task TurnTelemetryAsync(bool on)
    {
        return _settings.ReplaceAsync(_settings.Current with { TelemetryEnabled = on }, TestContext.Current.CancellationToken);
    }

    private void KeepAFileInTheStore()
    {
        Directory.CreateDirectory(StorageFolder);
        File.WriteAllText(Path.Combine(StorageFolder, "waiting.trn"), "kept by an earlier run");
    }

    private static Exception Thrown(Exception exception)
    {
        try
        {
            throw exception;
        }
        catch (Exception caught)
        {
            return caught;
        }
    }

    private static string? Message(JsonElement item)
    {
        var data = item.GetProperty("data").GetProperty("baseData");

        return data.GetProperty("exceptions")[0].GetProperty("message").GetString();
    }

    private static string Method(JsonElement frame)
    {
        return frame.GetProperty("method").GetString()!;
    }
}
