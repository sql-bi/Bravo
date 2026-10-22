using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Sqlbi.Bravo.Infrastructure.Diagnostics;

/// <summary>
/// Writes log records to the <see cref="DiagnosticLogStore"/> read by the diagnostics pane.
/// </summary>
[ProviderAlias("Diagnostics")]
internal sealed class DiagnosticLogProvider(DiagnosticLogStore store, TimeProvider timeProvider) : ILoggerProvider
{
    private readonly DiagnosticLogStore _store = store;
    private readonly TimeProvider _timeProvider = timeProvider;

    public ILogger CreateLogger(string categoryName) => new DiagnosticLogger(categoryName, _store, _timeProvider);

    public void Dispose()
    {
    }

    private sealed class DiagnosticLogger(string categoryName, DiagnosticLogStore store, TimeProvider timeProvider) : ILogger
    {
        private const string OriginalFormatKey = "{OriginalFormat}";

        private readonly string _categoryName = categoryName;
        private readonly DiagnosticLogStore _store = store;
        private readonly TimeProvider _timeProvider = timeProvider;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            try
            {
                var (template, properties) = ReadProperties(state);
                var entry = new DiagnosticLogEntry(
                    _timeProvider.GetUtcNow(),
                    logLevel,
                    _categoryName,
                    eventId.Id,
                    eventId.Name,
                    formatter(state, exception),
                    template,
                    properties,
                    DiagnosticLogException.CreateChain(exception));

                _store.Add(entry);
            }
            catch (Exception)
            {
                // A sink failure does not propagate to the caller of ILogger.Log.
            }
        }

        private static (string? Template, IReadOnlyDictionary<string, string?>? Properties) ReadProperties<TState>(TState state)
        {
            if (state is not IReadOnlyList<KeyValuePair<string, object?>> values)
            {
                return (null, null);
            }

            string? template = null;
            var properties = new Dictionary<string, string?>();
            foreach (var (key, value) in values)
            {
                if (key == OriginalFormatKey)
                {
                    template = value as string;
                }
                else
                {
                    properties[key] = value?.ToString();
                }
            }

            if (properties.Count == 0)
            {
                return (null, null);
            }

            return (template, properties);
        }
    }
}
