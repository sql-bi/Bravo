using System;
using Bravo.Tests.Host;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Infrastructure.Diagnostics;
using Xunit;

namespace Bravo.Tests.Infrastructure.Diagnostics;

public class UnhandledExceptionHandlerTests
{
    [Fact]
    public void Handle_UnobservedTaskBeforeAttachServices_DoesNotThrow()
    {
        var handler = new UnhandledExceptionHandler(new RecordingDialog());

        handler.Handle(new InvalidOperationException("unobserved"), ExceptionOrigin.UnobservedTask);
    }

    [Fact]
    public void Handle_UnobservedTaskAfterAttachServices_LogsAndTracks()
    {
        var loggerFactory = new FakeLoggerFactory();
        var telemetry = new FakeTelemetry();
        var handler = new UnhandledExceptionHandler(new RecordingDialog());
        var exception = new InvalidOperationException("unobserved");

        handler.AttachServices(loggerFactory, telemetry);
        handler.Handle(exception, ExceptionOrigin.UnobservedTask);

        var entry = Assert.Single(loggerFactory.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Same(exception, entry.Exception);
        Assert.Same(exception, Assert.Single(telemetry.Exceptions));
    }
}
