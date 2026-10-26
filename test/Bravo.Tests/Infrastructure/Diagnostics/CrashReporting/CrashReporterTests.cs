using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.DataContracts;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Sqlbi.Bravo.Infrastructure.Diagnostics.CrashReporting;
using Sqlbi.Bravo.Infrastructure.Telemetry;
using Xunit;

namespace Bravo.Tests.Infrastructure.Diagnostics.CrashReporting;

public sealed class CrashReporterTests : IDisposable
{
    private static readonly DateTimeOffset s_now = new(2026, 10, 22, 8, 30, 15, TimeSpan.Zero);

    private readonly TemporaryFolder _folder = new();
    private readonly CrashReportStore _store;
    private readonly ITelemetryService _telemetry = Substitute.For<ITelemetryService>();
    private readonly ITelemetryServiceFactory _telemetryFactory = Substitute.For<ITelemetryServiceFactory>();
    private readonly RecordingDialog _dialog = new();

    public CrashReporterTests()
    {
        _store = new CrashReportStore(Path.Combine(_folder.Path, "crashes"));
        _telemetryFactory.Create(Arg.Any<bool>()).Returns(_telemetry);
        _telemetry.FlushAsync(Arg.Any<CancellationToken>()).Returns(true);
    }

    public void Dispose() => _folder.Dispose();

    [Fact]
    public void Run_WithConsent_SendsNothingUntilTheUserAsks()
    {
        var reportPath = _store.Save(TestCrashReports.Create(s_now));

        CreateReporter().Run(reportPath);

        Assert.NotNull(_dialog.Presentation?.Send);
        _telemetryFactory.DidNotReceive().Create(Arg.Any<bool>());
    }

    [Fact]
    public async Task Run_WithConsent_SendsTheReportWhenTheUserAsks()
    {
        var reportPath = _store.Save(TestCrashReports.Create(s_now));
        CreateReporter().Run(reportPath);

        var sent = await _dialog.Presentation!.Send!();

        Assert.True(sent);
        _telemetryFactory.Received(1).Create(enabled: true);
        _telemetry.Received(1).TrackException(Arg.Any<ExceptionTelemetry>());
    }

    [Fact]
    public void Run_WithConsent_SendsOnceWhenAskedAgain()
    {
        var reportPath = _store.Save(TestCrashReports.Create(s_now));
        CreateReporter().Run(reportPath);
        var send = _dialog.Presentation!.Send!;

        var first = send();
        var second = send();

        Assert.Same(first, second);
        _telemetryFactory.Received(1).Create(enabled: true);
    }

    [Fact]
    public async Task Run_WithConsentWhenTelemetryCannotBeCreated_FailsTheSend()
    {
        var reportPath = _store.Save(TestCrashReports.Create(s_now));
        _telemetryFactory.Create(Arg.Any<bool>()).Throws(new InvalidOperationException("No telemetry."));
        CreateReporter().Run(reportPath);

        await Assert.ThrowsAsync<InvalidOperationException>(_dialog.Presentation!.Send!);
    }

    [Fact]
    public void Run_WithoutConsent_OffersNoWayToSend()
    {
        var reportPath = _store.Save(TestCrashReports.Create(s_now) with { TelemetryAllowed = false });

        CreateReporter().Run(reportPath);

        Assert.Null(_dialog.Presentation?.Send);
    }

    [Fact]
    public void Run_UnreadableReport_ShowsItWithNothingToSend()
    {
        var reportPath = _store.Save(TestCrashReports.Create(s_now));
        File.WriteAllText(reportPath, "{ not json");

        CreateReporter().Run(reportPath);

        Assert.Null(_dialog.Presentation?.Report);
        Assert.Null(_dialog.Presentation?.Send);
    }

    [Fact]
    public void Run_PrunesTheStoreAfterTheDialogCloses()
    {
        var reportPaths = Enumerable.Range(1, CrashReportStore.RetainedCount + 1)
            .Select((minute) => _store.Save(TestCrashReports.Create(s_now.AddMinutes(minute))))
            .ToArray();

        CreateReporter().Run(reportPaths[^1]);

        Assert.False(File.Exists(reportPaths[0]));
    }

    [Fact]
    public void Run_SendInProgressWhenTheDialogCloses_WaitsForItBeforeDisposingTheTelemetry()
    {
        var reportPath = _store.Save(TestCrashReports.Create(s_now));
        var flush = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _telemetry.FlushAsync(Arg.Any<CancellationToken>()).Returns(flush.Task);
        bool? flushedWhenDisposed = null;
        _telemetry.When((telemetry) => telemetry.Dispose()).Do((_) => flushedWhenDisposed = flush.Task.IsCompleted);

        // The send completes only after the dialog has closed: without the wait, Dispose would come first.
        var dialog = new SendingDialog(whileSending: () => _ = Task.Delay(20).ContinueWith((_) => flush.SetResult(true)));

        CreateReporter(dialog).Run(reportPath);

        Assert.True(flushedWhenDisposed);
        Assert.NotNull(dialog.Sending);
        Assert.True(dialog.Sending.IsCompletedSuccessfully);
    }

    [Fact]
    public void Run_SendThatFailed_EndsNormallyAndDisposesTheTelemetry()
    {
        var reportPath = _store.Save(TestCrashReports.Create(s_now));
        _telemetry.FlushAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException<bool>(new InvalidOperationException("Send failed.")));
        var dialog = new SendingDialog();

        CreateReporter(dialog).Run(reportPath);

        Assert.NotNull(dialog.Sending);
        Assert.True(dialog.Sending.IsFaulted);
        _telemetry.Received(1).Dispose();
    }

    private CrashReporter CreateReporter(ICrashReportDialog? dialog = null) => new(_store, _telemetryFactory, dialog ?? _dialog);

    private sealed class RecordingDialog : ICrashReportDialog
    {
        public CrashReportPresentation? Presentation { get; private set; }

        public void Show(CrashReportPresentation presentation) => Presentation = presentation;
    }

    // Starts a send and closes before it completes, as when the user clicks Send and then Close.
    private sealed class SendingDialog(Action? whileSending = null) : ICrashReportDialog
    {
        private readonly Action? _whileSending = whileSending;

        public Task<bool>? Sending { get; private set; }

        public void Show(CrashReportPresentation presentation)
        {
            Sending = presentation.Send!();
            _whileSending?.Invoke();
        }
    }
}
