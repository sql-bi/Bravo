using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Models;

namespace Sqlbi.Bravo.Infrastructure.Diagnostics;

internal sealed class DiagnosticMessageLogger(string categoryName) : ILogger
{
    private readonly string _categoryName = categoryName;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var content = formatter(state, exception);

        if (exception is not null)
            content = $"{content}{Environment.NewLine}{exception}";

        try
        {
            AppEnvironment.AddDiagnostics(DiagnosticMessageType.Text, _categoryName, content, GetSeverity(logLevel));
        }
        catch (Exception)
        {
            // A sink failure does not propagate to the caller of ILogger.Log.
        }
    }

    private static DiagnosticMessageSeverity GetSeverity(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Critical or LogLevel.Error => DiagnosticMessageSeverity.Error,
            LogLevel.Warning => DiagnosticMessageSeverity.Warning,
            _ => DiagnosticMessageSeverity.None,
        };
    }
}

internal static class DiagnosticMessageLoggerExtensions
{
    public static ILoggingBuilder AddDiagnosticMessages(this ILoggingBuilder builder)
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DiagnosticMessageLoggerProvider>());

        return builder;
    }
}
