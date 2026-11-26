using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Bravo.Api.Diagnostics;
using Bravo.Desktop.Development;
using Bravo.Desktop.Diagnostics;
using Bravo.Desktop.Telemetry;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Xunit;

namespace Bravo.Desktop.Tests.Diagnostics;

public sealed class CrashReportingTests : IDisposable
{
    private const string Version = "1.1.0";

    private static readonly TelemetrySession s_session = new("session-of-the-test");

    // What the reporter of a development build is started with: the options of the process that crashed.
    private static readonly DevelopmentOptions s_development = new() { TelemetryEndpoint = "http://127.0.0.1:8097" };

    private readonly TemporaryFolder _dataFolder = new();
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 11, 22, 9, 30, 15, TimeSpan.Zero));
    private readonly ICrashReporterLauncher _reporter = Substitute.For<ICrashReporterLauncher>();
    private readonly List<(Exception Exception, string? ReportPath)> _toldUser = [];
    private readonly InvalidOperationException _exception = new("It failed.");
    private readonly CrashReporting _reporting;

    public CrashReportingTests()
    {
        _reporter.TryStart(Arg.Any<string>(), Arg.Any<DevelopmentOptions>()).Returns(true);
        _reporting = new CrashReporting(
            _timeProvider,
            _reporter,
            (exception, reportPath) => _toldUser.Add((exception, reportPath)),
            () => new PersonalNames(@"C:\Users\jdoe", "jdoe", "LAPTOP-42"));
    }

    public void Dispose()
    {
        _dataFolder.Dispose();
    }

    [Fact]
    public void Handle_RunWithALog_WritesTheReportBesideTheLogAndStartsTheReporter()
    {
        _reporting.WriteReportsIn(_dataFolder.AsDataFolder(), Version, s_session, s_development);
        using var attached = _reporting.AttachLog(_dataFolder.Log);

        _reporting.Handle(_exception);

        var expectedPath = Path.ChangeExtension(_dataFolder.Log.FilePath!, ".crash.json");
        var report = CrashReportFile.Read(expectedPath);
        Assert.Equal((Version, "session-of-the-test", Path.GetFileName(_dataFolder.Log.FilePath)), (report.Version, report.Session, report.LogFile));
        Assert.Equal(_timeProvider.GetUtcNow(), report.Time);
        _reporter.Received(1).TryStart(expectedPath, s_development);
        Assert.Empty(_toldUser);
    }

    [Fact]
    public void Handle_RunWithALog_RecordsTheCrashWithTheTraceOfTheReport()
    {
        _reporting.WriteReportsIn(_dataFolder.AsDataFolder(), Version, s_session, s_development);
        using var attached = _reporting.AttachLog(_dataFolder.Log);

        _reporting.Handle(_exception);

        var record = Assert.Single(_dataFolder.Log.Buffer.Read(after: 0).Records);
        var reportPath = Path.ChangeExtension(_dataFolder.Log.FilePath!, ".crash.json");
        Assert.Equal(LogRecordLevel.Critical, record.Level);
        Assert.Contains(reportPath, record.Message, StringComparison.Ordinal);
        Assert.NotNull(record.TraceId);
        Assert.Equal(record.TraceId, CrashReportFile.Read(reportPath).Trace);
    }

    [Fact]
    public void Handle_InsideTheWorkThatFailed_SharesItsTrace()
    {
        _reporting.WriteReportsIn(_dataFolder.AsDataFolder(), Version, s_session, s_development);
        using var attached = _reporting.AttachLog(_dataFolder.Log);
        using var activity = new Activity("Bravo.Tests").Start();

        _reporting.Handle(_exception);

        var record = Assert.Single(_dataFolder.Log.Buffer.Read(after: 0).Records);
        Assert.Equal(activity.TraceId.ToHexString(), record.TraceId);
    }

    [Fact]
    public void Handle_BeforeTheDataFolderIsKnown_TellsTheUserWithoutAReport()
    {
        _reporting.Handle(_exception);

        Assert.Equal([(_exception, (string?)null)], _toldUser);
        _reporter.DidNotReceiveWithAnyArgs().TryStart(default!, default!);
    }

    [Fact]
    public void Handle_BeforeTheLogIsOpen_WritesAReportNamedAfterTheTimeOfTheCrash()
    {
        _reporting.WriteReportsIn(_dataFolder.AsDataFolder(), Version, s_session, s_development);

        _reporting.Handle(_exception);

        var reportPath = _dataFolder.Combine(HostLog.FolderName, $"Bravo-20261122-093015-{Environment.ProcessId}.crash.json");
        Assert.Null(CrashReportFile.Read(reportPath).LogFile);
        _reporter.Received(1).TryStart(reportPath, s_development);
    }

    [Fact]
    public void Handle_ReporterDoesNotStart_TellsTheUserWhereTheReportIs()
    {
        _reporter.TryStart(Arg.Any<string>(), Arg.Any<DevelopmentOptions>()).Returns(false);
        _reporting.WriteReportsIn(_dataFolder.AsDataFolder(), Version, s_session, s_development);

        _reporting.Handle(_exception);

        var (exception, reportPath) = Assert.Single(_toldUser);
        Assert.Same(_exception, exception);
        Assert.True(File.Exists(reportPath));
    }

    [Fact]
    public void Handle_ReportThatCannotBeWritten_RecordsWhyAndTellsTheUser()
    {
        // A file where the folder of the logs would be.
        var blocked = _dataFolder.Combine("blocked");
        File.WriteAllText(blocked, "Not a folder.");
        _reporting.WriteReportsIn(new DataFolder(blocked), Version, s_session, s_development);
        using var attached = _reporting.AttachLog(_dataFolder.Log);

        _reporting.Handle(_exception);

        var levels = _dataFolder.Log.Buffer.Read(after: 0).Records.Select(record => record.Level);
        Assert.Equal([LogRecordLevel.Warning, LogRecordLevel.Critical], levels);
        Assert.Equal([(_exception, (string?)null)], _toldUser);
        _reporter.DidNotReceiveWithAnyArgs().TryStart(default!, default!);
    }

    // Not only the disk: a text of the exception, or the names of the user, can fail to be read.
    [Fact]
    public void Handle_ReportThatCannotBeCreated_RecordsWhyAndTellsTheUser()
    {
        var reporting = new CrashReporting(
            _timeProvider,
            _reporter,
            (exception, reportPath) => _toldUser.Add((exception, reportPath)),
            () => throw new InvalidOperationException("The names cannot be read."));
        reporting.WriteReportsIn(_dataFolder.AsDataFolder(), Version, s_session, s_development);
        using var attached = reporting.AttachLog(_dataFolder.Log);

        reporting.Handle(_exception);

        var levels = _dataFolder.Log.Buffer.Read(after: 0).Records.Select(record => record.Level);
        Assert.Equal([LogRecordLevel.Warning, LogRecordLevel.Critical], levels);
        Assert.Equal([(_exception, (string?)null)], _toldUser);
    }

    [Fact]
    public void Handle_AfterTheLogIsDetached_RecordsNothing()
    {
        _reporting.WriteReportsIn(_dataFolder.AsDataFolder(), Version, s_session, s_development);
        _reporting.AttachLog(_dataFolder.Log).Dispose();

        _reporting.Handle(_exception);

        Assert.Empty(_dataFolder.Log.Buffer.Read(after: 0).Records);
    }
}
