using System;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Infrastructure.Diagnostics;
using Xunit;

namespace Bravo.Tests.Infrastructure.Diagnostics;

public class DiagnosticLogProviderTests
{
    private static readonly DateTimeOffset s_now = new(2026, 10, 21, 8, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Log_StoresTheRecordWithItsTemplateAndProperties()
    {
        var store = new DiagnosticLogStore();
        using var loggerFactory = CreateLoggerFactory(store);
        var logger = loggerFactory.CreateLogger("Sqlbi.Bravo.Test");

        logger.Log(LogLevel.Information, new EventId(7, "LogPayloadReceived"), "Payload received: {Payload}", "[1,2]");

        var entry = Assert.Single(store.Read(after: 0).Entries);
        Assert.Equal(s_now, entry.Timestamp);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal("Sqlbi.Bravo.Test", entry.Category);
        Assert.Equal(7, entry.EventId);
        Assert.Equal("LogPayloadReceived", entry.EventName);
        Assert.Equal("Payload received: [1,2]", entry.Message);
        Assert.Equal("Payload received: {Payload}", entry.Template);
        Assert.NotNull(entry.Properties);
        Assert.Equal("[1,2]", entry.Properties["Payload"]);
        Assert.Null(entry.Exception);
    }

    [Fact]
    public void Log_OmitsTheTemplateOfAMessageWithoutPlaceholders()
    {
        var store = new DiagnosticLogStore();
        using var loggerFactory = CreateLoggerFactory(store);
        var logger = loggerFactory.CreateLogger("Sqlbi.Bravo.Test");

        logger.LogWarning("plain message");

        var entry = Assert.Single(store.Read(after: 0).Entries);
        Assert.Equal("plain message", entry.Message);
        Assert.Null(entry.Template);
        Assert.Null(entry.Properties);
    }

    [Fact]
    public void Log_StoresTheExceptionChainOutermostFirst()
    {
        var store = new DiagnosticLogStore();
        using var loggerFactory = CreateLoggerFactory(store);
        var logger = loggerFactory.CreateLogger("Sqlbi.Bravo.Test");
        var exception = new InvalidOperationException("outer", new TimeoutException("inner"));

        logger.LogError(exception, "the message");

        var entry = Assert.Single(store.Read(after: 0).Entries);
        Assert.Equal("the message", entry.Message);
        Assert.NotNull(entry.Exception);
        Assert.Collection(
            entry.Exception,
            (outer) => Assert.Equal((typeof(InvalidOperationException).FullName, "outer"), (outer.Type, outer.Message)),
            (inner) => Assert.Equal((typeof(TimeoutException).FullName, "inner"), (inner.Type, inner.Message)));
    }

    [Fact]
    public void Log_KeepsTheTextInsideSensitiveTagsForThePane()
    {
        var store = new DiagnosticLogStore();
        using var loggerFactory = CreateLoggerFactory(store);
        var logger = loggerFactory.CreateLogger("Sqlbi.Bravo.Test");
        var exception = new InvalidOperationException("user <pii>alice</pii> denied");

        logger.LogWarning(exception, "Database {Database} not found", "<pii>Sales</pii>");

        var entry = Assert.Single(store.Read(after: 0).Entries);
        Assert.Equal("Database <pii>Sales</pii> not found", entry.Message);
        Assert.NotNull(entry.Properties);
        Assert.Equal("<pii>Sales</pii>", entry.Properties["Database"]);
        Assert.NotNull(entry.Exception);
        Assert.Equal("user <pii>alice</pii> denied", entry.Exception[0].Message);
    }

    private static ILoggerFactory CreateLoggerFactory(DiagnosticLogStore store)
    {
        var timeProvider = new FixedTimeProvider(s_now);
        return LoggerFactory.Create((builder) => builder.AddDiagnosticLog(store, timeProvider));
    }
}
