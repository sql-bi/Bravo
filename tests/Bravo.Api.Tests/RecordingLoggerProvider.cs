using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Bravo.Api.Tests;

/// <summary>
/// What the API wrote to its log, every level and every category.
/// </summary>
internal sealed class RecordingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<RecordedLog> _records = new();

    public IReadOnlyList<RecordedLog> Records => [.. _records];

    public ILogger CreateLogger(string categoryName)
    {
        return new RecordingLogger(categoryName, _records);
    }

    public void Dispose()
    {
    }

    private sealed class RecordingLogger(string category, ConcurrentQueue<RecordedLog> records) : ILogger
    {
        private readonly string _category = category;
        private readonly ConcurrentQueue<RecordedLog> _records = records;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _records.Enqueue(new RecordedLog(logLevel, _category, exception));
        }
    }
}

internal sealed record RecordedLog(LogLevel Level, string Category, Exception? Exception);
