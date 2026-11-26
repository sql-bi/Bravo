using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Bravo.Core.Settings;
using Bravo.Desktop.Development;
using Bravo.Desktop.Diagnostics;
using Bravo.Desktop.Telemetry;
using Bravo.Desktop.Tests.Telemetry;
using Bravo.Platform.Windows.Tests.Network;
using Microsoft.Win32;
using Xunit;

namespace Bravo.Desktop.Tests.Diagnostics;

/// <summary>
/// The telemetry of the reporter, with the policies, the settings and the store of a development build in a
/// folder and a key of the test, and an endpoint of the test on the loopback address.
/// </summary>
public sealed class CrashReportSubmissionTests : IAsyncDisposable
{
    private const string Trace = "4bf92f3577b34da6a3ce929d0e0e4736";

    private static readonly BuildIdentity s_identity = BuildIdentity.Development("1.1.0");
    private static readonly DateTimeOffset s_time = new(2026, 11, 22, 9, 30, 15, TimeSpan.Zero);

    private readonly TemporaryFolder _dataFolder = new();
    private readonly TemporaryRegistryKey _registry = new();
    private readonly TestIngestion _ingestion = new();
    private readonly CrashReport _report = CrashReport.Create(
        new InvalidOperationException("It failed."),
        "1.1.0",
        new TelemetrySession("session-of-the-crash"),
        s_time,
        Trace,
        logFile: null,
        new PersonalNames(@"C:\Users\canary-user", "canary-user", "CANARY-PC"));

    private string StorageFolder => _dataFolder.Combine("Telemetry");

    public async ValueTask DisposeAsync()
    {
        await _ingestion.DisposeAsync();
        _registry.Dispose();
        _dataFolder.Dispose();
    }

    [Fact]
    public void For_BuildWithoutTelemetry_SendsNothing()
    {
        var submission = For(new DevelopmentOptions { DataFolderPath = _dataFolder.Path });

        Assert.Equal(CrashReportSubmissionMode.NoTelemetry, submission.Mode);
        Assert.Throws<InvalidOperationException>(() => { _ = submission.SendAsync(); });
    }

    [Fact]
    public void For_ReportOfAnEarlierBuild_SendsNothing()
    {
        var report = _report with { Format = 1, Session = null, Exception = _report.Exception with { Chain = null } };

        Assert.Equal(CrashReportSubmissionMode.EarlierFormat, For(Options(), report).Mode);
    }

    [Fact]
    public void For_PoliciesThatCannotBeRead_SendsNothing()
    {
        var options = Options() with { PolicyKeyName = _registry.DenyReading("Policies") };

        Assert.Equal(CrashReportSubmissionMode.PoliciesUnreadable, For(options).Mode);
    }

    [Fact]
    public void For_PolicyThatTurnsTelemetryOff_SendsNothingWhateverTheOption()
    {
        WriteSettings("""{ "telemetryEnabled": true }""");

        Assert.Equal(CrashReportSubmissionMode.Forbidden, For(Options() with { PolicyKeyName = SetPolicy(0) }).Mode);
    }

    [Fact]
    public void For_PolicyThatTurnsTelemetryOn_SendsByItselfWhateverTheOption()
    {
        WriteSettings("""{ "telemetryEnabled": false }""");

        Assert.Equal(CrashReportSubmissionMode.Automatic, For(Options() with { PolicyKeyName = SetPolicy(1) }).Mode);
    }

    [Theory]
    [InlineData("""{ "telemetryEnabled": true }""", CrashReportSubmissionMode.Automatic)]
    [InlineData("""{ "telemetryEnabled": false }""", CrashReportSubmissionMode.OnRequest)]
    public void For_OptionOfTheUser_DecidesWithoutAPolicy(string settings, int expected)
    {
        WriteSettings(settings);

        Assert.Equal((CrashReportSubmissionMode)expected, For(Options()).Mode);
    }

    // As the host: a user without settings has the choice of the installation, and without one telemetry is on.
    [Fact]
    public void For_NoSettings_SendsByItself()
    {
        Assert.Equal(CrashReportSubmissionMode.Automatic, For(Options()).Mode);
    }

    // A file that cannot be read gives no consent, and the reporter neither replaces nor moves it.
    [Fact]
    public void For_SettingsThatCannotBeRead_SendsOnRequestAndLeavesTheFile()
    {
        WriteSettings("not settings");

        Assert.Equal(CrashReportSubmissionMode.OnRequest, For(Options()).Mode);
        Assert.Equal(["usersettings.json"], Array.ConvertAll(Directory.GetFiles(_dataFolder.Path), Path.GetFileName));
        Assert.Equal("not settings", File.ReadAllText(_dataFolder.Combine("usersettings.json")));
    }

    // The crash joins the telemetry of the run that crashed: its session, its trace, its time.
    [Fact]
    public async Task SendAsync_Automatic_SendsTheCrashAsAnItemOfTheRunThatCrashed()
    {
        var delivery = await For(Options()).SendAsync();

        var item = Assert.Single(await _ingestion.WaitForItemsAsync(1));
        Assert.Equal(TelemetryDelivery.Sent, delivery);
        Assert.Equal(s_time, DateTimeOffset.Parse(item.GetProperty("time").GetString()!, CultureInfo.InvariantCulture));
        var tags = item.GetProperty("tags");
        Assert.Equal(("session-of-the-crash", Trace), (tags.GetProperty("ai.session.id").GetString(), tags.GetProperty("ai.operation.id").GetString()));
        var data = item.GetProperty("data").GetProperty("baseData");
        Assert.Equal("Unhandled", data.GetProperty("properties").GetProperty("ExceptionKind").GetString());
        var exception = Assert.Single(data.GetProperty("exceptions").EnumerateArray());
        Assert.Equal(("System.InvalidOperationException", "It failed."), (exception.GetProperty("typeName").GetString(), exception.GetProperty("message").GetString()));
    }

    // The store keeps it, and the next start of Bravo that has the consent sends it.
    [Fact]
    public async Task SendAsync_AutomaticToAnEndpointThatCannotBeReached_KeepsTheCrashInTheStore()
    {
        var delivery = await For(Options() with { TelemetryEndpoint = ClosedEndpoint() }).SendAsync();

        Assert.Equal(TelemetryDelivery.Kept, delivery);
        Assert.NotEmpty(Directory.GetFiles(StorageFolder, "*.trn"));
    }

    [Fact]
    public async Task SendAsync_OnRequest_SendsTheCrashAndKeepsNothing()
    {
        WriteSettings("""{ "telemetryEnabled": false }""");

        var delivery = await For(Options()).SendAsync();

        Assert.Equal(TelemetryDelivery.Sent, delivery);
        Assert.Single(await _ingestion.WaitForItemsAsync(1));
        Assert.Empty(Directory.GetFiles(StorageFolder));
    }

    // It waits in the store of the host, as an automatic send would, and the next start with telemetry off deletes
    // it: for the user it is not sent.
    [Fact]
    public async Task SendAsync_OnRequestToAnEndpointThatCannotBeReached_KeepsTheCrashInTheStoreAndSaysNotSent()
    {
        WriteSettings("""{ "telemetryEnabled": false }""");

        var delivery = await For(Options() with { TelemetryEndpoint = ClosedEndpoint() }).SendAsync();

        Assert.Equal(TelemetryDelivery.NotSent, delivery);
        Assert.NotEmpty(Directory.GetFiles(StorageFolder, "*.trn"));
    }

    // A second send would find the crash of the first in the store as well, and send both.
    [Fact]
    public async Task SendAsync_Again_ReturnsTheFirstSend()
    {
        var submission = For(Options());

        var first = submission.SendAsync();
        var whileSending = submission.SendAsync();
        await first;
        var afterIt = submission.SendAsync();

        Assert.Same(first, whileSending);
        Assert.Same(first, afterIt);
        Assert.Equal(TelemetryDelivery.Sent, await afterIt);
    }

    // The proxy is given to the process once its settings are read, before the send.
    [Fact]
    public async Task SendAsync_ProxyOfTheSettings_IsGivenBeforeTheSend()
    {
        WriteSettings("""{ "telemetryEnabled": true, "proxy": { "type": 0 } }""");
        ProxySettings? given = null;
        var submission = CrashReportSubmission.For(_report, Start(Options()), s_identity, (settings, _) => given = settings);

        await submission.SendAsync();

        Assert.Equal(ProxyType.None, given?.Type);
    }

    private static string ClosedEndpoint()
    {
        return $"http://127.0.0.1:{TestProxy.ClosedAddress().Port}";
    }

    private DevelopmentOptions Options()
    {
        return new DevelopmentOptions { DataFolderPath = _dataFolder.Path, TelemetryEndpoint = _ingestion.Origin };
    }

    private CrashReporterStart Start(DevelopmentOptions options)
    {
        return new CrashReporterStart(_dataFolder.Combine("Logs", "Bravo-20261122-093015-42.crash.json"), options);
    }

    // The test process keeps its own proxy.
    private CrashReportSubmission For(DevelopmentOptions options, CrashReport? report = null)
    {
        return CrashReportSubmission.For(report ?? _report, Start(options), s_identity, static (_, _) => { });
    }

    private void WriteSettings(string text)
    {
        File.WriteAllText(_dataFolder.Combine("usersettings.json"), text);
    }

    private string SetPolicy(int telemetryEnabled)
    {
        _registry.SetValue("Policies", "TelemetryEnabled", telemetryEnabled, RegistryValueKind.DWord);

        return _registry.SubKeyPath("Policies");
    }
}
