using System;
using System.IO;
using Bravo.Tests.Host;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Infrastructure.Diagnostics;
using Sqlbi.Bravo.Infrastructure.Telemetry;
using Xunit;

namespace Bravo.Tests.Infrastructure.Diagnostics;

public sealed class UnhandledExceptionHandlerTests : IDisposable
{
    private readonly string _reportDirectory = Path.Combine(Path.GetTempPath(), $"Bravo.Tests.{Guid.NewGuid():N}");

    [Fact]
    public void Handle_FatalBeforeAttachServices_SavesReportAndShowsDialogWithoutTelemetry()
    {
        var dialog = new RecordingDialog();
        var handler = CreateHandler(dialog);

        handler.Handle(new InvalidOperationException("fatal"), ExceptionOrigin.AppDomain);

        Assert.Equal(1, dialog.ShowCount);
        Assert.Null(dialog.LastTelemetry);
        Assert.True(File.Exists(dialog.LastReport?.FilePath));
    }

    [Fact]
    public void Handle_FatalAfterAttachServices_LogsTracksAndPassesTelemetryToDialog()
    {
        var loggerFactory = new FakeLoggerFactory();
        var telemetry = new FakeTelemetry();
        var dialog = new RecordingDialog();
        var handler = CreateHandler(dialog);
        var exception = new InvalidOperationException("fatal");

        using var _ = handler.AttachServices(loggerFactory, telemetry);
        handler.Handle(exception, ExceptionOrigin.AppDomain);

        Assert.Same(exception, Assert.Single(loggerFactory.Entries).Exception);
        Assert.Same(exception, Assert.Single(telemetry.Exceptions));
        Assert.Same(telemetry, dialog.LastTelemetry);
    }

    [Fact]
    public void Handle_DialogThrows_LogsTheFailureWithoutThrowing()
    {
        var loggerFactory = new FakeLoggerFactory();
        var dialogFailure = new InvalidOperationException("dialog");
        var handler = CreateHandler(new RecordingDialog { ExceptionToThrow = dialogFailure });

        using var _ = handler.AttachServices(loggerFactory, new FakeTelemetry());
        handler.Handle(new InvalidOperationException("fatal"), ExceptionOrigin.AppDomain);

        Assert.Contains(loggerFactory.Entries, entry => ReferenceEquals(entry.Exception, dialogFailure));
    }

    [Fact]
    public void Handle_FatalAfterAttachmentDisposed_DoesNotUseTheServices()
    {
        var loggerFactory = new FakeLoggerFactory();
        var telemetry = new FakeTelemetry();
        var dialog = new RecordingDialog();
        var handler = CreateHandler(dialog);

        handler.AttachServices(loggerFactory, telemetry).Dispose();
        handler.Handle(new InvalidOperationException("fatal"), ExceptionOrigin.AppDomain);

        Assert.Empty(loggerFactory.Entries);
        Assert.Empty(telemetry.Exceptions);
        Assert.Null(dialog.LastTelemetry);
    }

    [Fact]
    public void Handle_EarlierAttachmentDisposed_KeepsTheLaterServices()
    {
        var telemetry = new FakeTelemetry();
        var dialog = new RecordingDialog();
        var handler = CreateHandler(dialog);
        var earlier = handler.AttachServices(new FakeLoggerFactory(), new FakeTelemetry());

        using var _ = handler.AttachServices(new FakeLoggerFactory(), telemetry);
        earlier.Dispose();
        handler.Handle(new InvalidOperationException("fatal"), ExceptionOrigin.AppDomain);

        Assert.Same(telemetry, dialog.LastTelemetry);
    }

    [Fact]
    public void Handle_AttachedNullTelemetryService_PassesNoTelemetryToDialog()
    {
        var dialog = new RecordingDialog();
        var handler = CreateHandler(dialog);

        using var _ = handler.AttachServices(new FakeLoggerFactory(), new NullTelemetryService());
        handler.Handle(new InvalidOperationException("fatal"), ExceptionOrigin.AppDomain);

        Assert.Null(dialog.LastTelemetry);
    }

    [Fact]
    public void Handle_ReportDirectoryUnavailable_ShowsDialogWithoutFile()
    {
        var dialog = new RecordingDialog();
        var handler = new UnhandledExceptionHandler(dialog, () => throw new TypeInitializationException("AppEnvironment", null));

        handler.Handle(new InvalidOperationException("fatal"), ExceptionOrigin.AppDomain);

        Assert.Equal(1, dialog.ShowCount);
        Assert.Null(dialog.LastReport?.FilePath);
    }

    [Fact]
    public void Handle_UnobservedTaskAfterAttachServices_LogsAndTracksWithoutDialog()
    {
        var loggerFactory = new FakeLoggerFactory();
        var telemetry = new FakeTelemetry();
        var dialog = new RecordingDialog();
        var handler = CreateHandler(dialog);
        var exception = new InvalidOperationException("unobserved");

        using var _ = handler.AttachServices(loggerFactory, telemetry);
        handler.Handle(exception, ExceptionOrigin.UnobservedTask);

        var entry = Assert.Single(loggerFactory.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Same(exception, Assert.Single(telemetry.Exceptions));
        Assert.Equal(0, dialog.ShowCount);
    }

    public void Dispose()
    {
        if (Directory.Exists(_reportDirectory))
            Directory.Delete(_reportDirectory, recursive: true);
    }

    private UnhandledExceptionHandler CreateHandler(IErrorReportDialog dialog) => new(dialog, () => _reportDirectory);
}
