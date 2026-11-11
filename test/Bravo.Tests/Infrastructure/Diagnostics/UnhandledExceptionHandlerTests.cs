using System;
using System.Collections.Generic;
using System.IO;
using Bravo.Tests.Host;
using Bravo.Tests.Infrastructure.Diagnostics.CrashReporting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Sqlbi.Bravo.Infrastructure.Diagnostics;
using Sqlbi.Bravo.Infrastructure.Diagnostics.CrashReporting;
using Sqlbi.Bravo.Infrastructure.Telemetry;
using Xunit;

namespace Bravo.Tests.Infrastructure.Diagnostics;

public sealed class UnhandledExceptionHandlerTests : IDisposable
{
    private readonly TemporaryFolder _folder = new();
    private readonly string _directoryPath;
    private readonly ICrashReporterLauncher _launcher = Substitute.For<ICrashReporterLauncher>();
    private readonly List<Exception> _fallbacks = [];

    public UnhandledExceptionHandlerTests()
    {
        _directoryPath = Path.Combine(_folder.Path, "crashes");
    }

    public void Dispose() => _folder.Dispose();

    [Fact]
    public void Handle_FatalBeforeAttachServices_SavesAReportNotSentAndStartsTheReporter()
    {
        CreateHandler().Handle(new InvalidOperationException("fatal"), ExceptionOrigin.AppDomain);

        var report = LoadSingleReport(out var reportPath);
        _launcher.Received(1).Launch(reportPath);
        Assert.False(report.TelemetryAllowed);
        Assert.Null(report.SessionId);
    }

    [Fact]
    public void Handle_FatalAfterAttachServices_RecordsTheTelemetryConsentTheSessionAndTheTimeAndLogs()
    {
        var loggerFactory = new FakeLoggerFactory();
        var exception = new InvalidOperationException("fatal");
        var handler = CreateHandler();
        var telemetry = new FakeTelemetry { TelemetryEnabled = true, SessionId = TestCrashReports.SessionId };

        using var attachment = handler.AttachServices(loggerFactory, telemetry);
        handler.Handle(exception, ExceptionOrigin.AppDomain);

        var report = LoadSingleReport(out _);
        Assert.True(report.TelemetryAllowed);
        Assert.Equal(TestCrashReports.SessionId, report.SessionId);
        Assert.Equal(TestCrashReports.Timestamp, report.Timestamp);
        Assert.Same(exception, Assert.Single(loggerFactory.Entries).Exception);
    }

    [Fact]
    public void Handle_FatalAfterTheUserTurnedTelemetryOff_RecordsTheConsentOfTheMomentOfTheCrash()
    {
        var telemetry = new FakeTelemetry { TelemetryEnabled = true };
        var handler = CreateHandler();

        using var attachment = handler.AttachServices(new FakeLoggerFactory(), telemetry);
        telemetry.TelemetryEnabled = false;
        handler.Handle(new InvalidOperationException("fatal"), ExceptionOrigin.AppDomain);

        Assert.False(LoadSingleReport(out _).TelemetryAllowed);
    }

    [Fact]
    public void Handle_Fatal_SendsNothingFromTheProcessThatCrashed()
    {
        var telemetry = new FakeTelemetry { TelemetryEnabled = true };
        var handler = CreateHandler();

        using var attachment = handler.AttachServices(new FakeLoggerFactory(), telemetry);
        handler.Handle(new InvalidOperationException("fatal"), ExceptionOrigin.AppDomain);

        Assert.Empty(telemetry.Exceptions);
    }

    [Fact]
    public void Handle_FatalWhenTheReporterFailsToStart_ShowsTheFallbackWithoutSending()
    {
        var loggerFactory = new FakeLoggerFactory();
        var telemetry = new FakeTelemetry { TelemetryEnabled = true };
        var exception = new InvalidOperationException("fatal");
        MakeTheLauncherFail();
        var handler = CreateHandler();

        using var attachment = handler.AttachServices(loggerFactory, telemetry);
        handler.Handle(exception, ExceptionOrigin.AppDomain);

        Assert.Empty(telemetry.Exceptions);
        Assert.Same(exception, Assert.Single(_fallbacks));
        Assert.Contains(loggerFactory.Entries, (entry) => entry.Exception?.Message == "cannot start");
    }

    [Fact]
    public void Handle_FatalWhenTheReporterFailsToStart_LogsTheCrashBeforeTheFallbackWaitsForTheUser()
    {
        var loggerFactory = new FakeLoggerFactory();
        var exception = new InvalidOperationException("fatal");
        MakeTheLauncherFail();
        IReadOnlyList<FakeLogEntry> entriesWhenShown = [];
        var handler = CreateHandler(showFallback: (_) => entriesWhenShown = loggerFactory.Entries);

        using var attachment = handler.AttachServices(loggerFactory, new FakeTelemetry());
        handler.Handle(exception, ExceptionOrigin.AppDomain);

        Assert.Contains(entriesWhenShown, (entry) => entry.Exception == exception);
    }

    [Fact]
    public void Handle_UnobservedTask_LogsAndTracksWithoutACrashReportOrFlush()
    {
        var loggerFactory = new FakeLoggerFactory();
        var telemetry = Substitute.For<ITelemetryService>();
        var exception = new InvalidOperationException("unobserved");
        var handler = CreateHandler();

        using var attachment = handler.AttachServices(loggerFactory, telemetry);
        handler.Handle(exception, ExceptionOrigin.UnobservedTask);

        Assert.Equal(LogLevel.Error, Assert.Single(loggerFactory.Entries).Level);
        telemetry.Received(1).TrackException(exception, Arg.Is<IDictionary<string, string>>(
            (properties) => properties["ExceptionOrigin"] == nameof(ExceptionOrigin.UnobservedTask)));
        telemetry.DidNotReceive().FlushAsync(Arg.Any<TimeSpan>());
        Assert.False(Directory.Exists(_directoryPath));
    }

    [Fact]
    public void Handle_AfterTheAttachmentIsDisposed_DoesNotUseTheServices()
    {
        var loggerFactory = new FakeLoggerFactory();
        var handler = CreateHandler();

        handler.AttachServices(loggerFactory, new FakeTelemetry { TelemetryEnabled = true }).Dispose();
        handler.Handle(new InvalidOperationException("fatal"), ExceptionOrigin.AppDomain);

        Assert.Empty(loggerFactory.Entries);
        Assert.False(LoadSingleReport(out _).TelemetryAllowed);
    }

    [Fact]
    public void Handle_AfterAnEarlierAttachmentIsDisposed_KeepsTheLaterServices()
    {
        var loggerFactory = new FakeLoggerFactory();
        var handler = CreateHandler();
        var earlier = handler.AttachServices(new FakeLoggerFactory(), new FakeTelemetry());

        using var attachment = handler.AttachServices(loggerFactory, new FakeTelemetry());
        earlier.Dispose();
        handler.Handle(new InvalidOperationException("fatal"), ExceptionOrigin.AppDomain);

        Assert.Single(loggerFactory.Entries);
    }

    private UnhandledExceptionHandler CreateHandler(Action<Exception>? showFallback = null)
    {
        var timeProvider = new FixedTimeProvider(TestCrashReports.Timestamp);

        return new UnhandledExceptionHandler(new CrashReportStore(_directoryPath), _launcher, timeProvider, showFallback ?? _fallbacks.Add);
    }

    private void MakeTheLauncherFail()
    {
        _launcher.When((launcher) => launcher.Launch(Arg.Any<string>())).Throw(new InvalidOperationException("cannot start"));
    }

    private CrashReport LoadSingleReport(out string reportPath)
    {
        reportPath = Assert.Single(Directory.GetFiles(_directoryPath));
        Assert.True(new CrashReportStore(_directoryPath).TryLoad(reportPath, out var report));

        return report;
    }
}
