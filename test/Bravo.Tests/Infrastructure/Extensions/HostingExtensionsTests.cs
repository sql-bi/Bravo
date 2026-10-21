using System;
using Bravo.Tests.Host;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Infrastructure.Extensions;
using Sqlbi.Bravo.Infrastructure.Telemetry;
using Xunit;

namespace Bravo.Tests.Infrastructure.Extensions;

public class HostingExtensionsTests
{
    [Fact]
    public void ReportMappedException_LogsAndTracksTheException()
    {
        var loggerFactory = new FakeLoggerFactory();
        var telemetry = new FakeTelemetry();
        var services = new ServiceCollection()
            .AddSingleton<ILoggerFactory>(loggerFactory)
            .AddSingleton<ITelemetryService>(telemetry)
            .BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        var exception = new InvalidOperationException("connection failed");

        HostingExtensions.ReportMappedException(context, exception);

        var entry = Assert.Single(loggerFactory.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Same(exception, entry.Exception);
        Assert.Same(exception, Assert.Single(telemetry.Exceptions));
    }
}
