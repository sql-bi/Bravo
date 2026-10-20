using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Sqlbi.Bravo.Host;
using Sqlbi.Bravo.Infrastructure.Telemetry;

namespace Bravo.Tests.Host;

// Avoid claiming the production named pipe in tests.
internal sealed class FakeInstance : IBravoApplicationInstance
{
    public bool IsPrimary { get; init; }
    public int DisposeCount { get; private set; }
    public Action? OnDispose { get; init; }

    public event EventHandler<InstanceActivationRequestedEventArgs>? ActivationRequested
    {
        add { }
        remove { }
    }

    public void RedirectActivationToPrimary() { }

    public void Dispose()
    {
        DisposeCount++;
        OnDispose?.Invoke();
    }
}

internal sealed class FakeTelemetry : ITelemetryService
{
    private readonly List<Exception> _exceptions = [];

    public int DisposeCount { get; private set; }
    public Action? OnDispose { get; init; }
    public bool TelemetryEnabled { get; set; }

    public IReadOnlyList<Exception> Exceptions
    {
        get { lock (_exceptions) return [.. _exceptions]; }
    }

    public void TrackException(Exception exception, IDictionary<string, string>? properties = null)
    {
        lock (_exceptions) _exceptions.Add(exception);
    }

    public Task<bool> FlushAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);

    public bool TryFlushBeforeShutdown(int timeoutMilliseconds) => false;

    public void Dispose()
    {
        DisposeCount++;
        OnDispose?.Invoke();
    }
}

internal sealed record FakeLogEntry(LogLevel Level, EventId EventId, Exception? Exception, string Message);

internal sealed class FakeLoggerFactory : ILoggerFactory
{
    private readonly List<FakeLogEntry> _entries = [];

    public int DisposeCount { get; private set; }

    public IReadOnlyList<FakeLogEntry> Entries
    {
        get { lock (_entries) return [.. _entries]; }
    }

    public void AddProvider(ILoggerProvider provider) { }

    public ILogger CreateLogger(string categoryName) => new FakeLogger(this);

    public void Dispose() => DisposeCount++;

    private sealed class FakeLogger(FakeLoggerFactory owner) : ILogger
    {
        private readonly FakeLoggerFactory _owner = owner;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var entry = new FakeLogEntry(logLevel, eventId, exception, formatter(state, exception));
            lock (_owner._entries) _owner._entries.Add(entry);
        }
    }
}
