using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Bravo.Infrastructure.Diagnostics;
using Sqlbi.Bravo.Models;
using Xunit;

namespace Bravo.Tests.Infrastructure.Diagnostics;

public class DiagnosticMessageLoggerProviderTests
{
    [Theory]
    [InlineData(LogLevel.Critical, DiagnosticMessageSeverity.Error)]
    [InlineData(LogLevel.Error, DiagnosticMessageSeverity.Error)]
    [InlineData(LogLevel.Warning, DiagnosticMessageSeverity.Warning)]
    [InlineData(LogLevel.Information, DiagnosticMessageSeverity.None)]
    public void Log_MapsTheLevelToTheMessageSeverity(LogLevel logLevel, DiagnosticMessageSeverity expected)
    {
        var category = $"{nameof(Log_MapsTheLevelToTheMessageSeverity)}.{logLevel}";
        using var provider = new DiagnosticMessageLoggerProvider();
        var logger = provider.CreateLogger(category);

        logger.Log(logLevel, default, "the message", null, (state, _) => state);

        var message = FindMessage(category);
        Assert.Equal(expected, message.Severity);
        Assert.Equal(DiagnosticMessageType.Text, message.Type);
        Assert.Equal("the message", message.Content);
    }

    [Fact]
    public void Log_AppendsTheExceptionToTheContent()
    {
        var category = nameof(Log_AppendsTheExceptionToTheContent);
        var exception = new InvalidOperationException("policies unavailable");
        using var provider = new DiagnosticMessageLoggerProvider();
        var logger = provider.CreateLogger(category);

        logger.LogError(exception, "the message");

        var message = FindMessage(category);
        Assert.StartsWith("the message", message.Content);
        Assert.Contains("policies unavailable", message.Content);
    }

    private static DiagnosticMessage FindMessage(string category)
    {
        var message = AppEnvironment.Diagnostics.Values.SingleOrDefault((m) => m.Name == $"[HOST] {category}");

        Assert.NotNull(message);
        Assert.NotNull(message.Content);

        return message;
    }
}
