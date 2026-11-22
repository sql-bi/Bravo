using System;
using Bravo.Desktop.Diagnostics;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Bravo.Desktop.Tests.Diagnostics;

public sealed class GuardedLoggerProviderTests
{
    [Fact]
    public void Log_LoggerThatThrows_DoesNotThrowToTheCaller()
    {
        using var provider = new GuardedLoggerProvider(new ThrowingLoggerProvider());
        var logger = provider.CreateLogger("Bravo.Tests");

        var exception = Record.Exception(() => logger.Log(LogLevel.Warning, default, "Lost.", null, (state, _) => state));

        Assert.Null(exception);
    }

    // The scopes carry the trace of the record, which the Event Log writes in the text of its entry.
    [Fact]
    public void SetScopeProvider_ProviderThatWritesScopes_GivesItTheScopesOfTheFactory()
    {
        var inner = new ScopeRecordingLoggerProvider();
        using var provider = new GuardedLoggerProvider(inner);
        var scopes = new LoggerExternalScopeProvider();

        provider.SetScopeProvider(scopes);

        Assert.Same(scopes, inner.ScopeProvider);
    }

    private sealed class ThrowingLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
        {
            return new ThrowingLogger();
        }

        public void Dispose()
        {
        }
    }

    private sealed class ScopeRecordingLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        public IExternalScopeProvider? ScopeProvider { get; private set; }

        public ILogger CreateLogger(string categoryName)
        {
            return new ThrowingLogger();
        }

        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            ScopeProvider = scopeProvider;
        }

        public void Dispose()
        {
        }
    }

    private sealed class ThrowingLogger : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            throw new InvalidOperationException("The log cannot be written.");
        }
    }
}
