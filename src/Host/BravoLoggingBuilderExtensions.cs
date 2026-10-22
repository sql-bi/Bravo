using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using Sqlbi.Bravo.Infrastructure.Diagnostics;

namespace Sqlbi.Bravo.Host;

internal static class BravoLoggingBuilderExtensions
{
    // Shared by the primary and the secondary instance, so that both write to the Event Log with the same source and threshold.
    public static ILoggingBuilder AddBravoEventLog(this ILoggingBuilder builder)
    {
        builder.AddEventLog();
        builder.AddFilter<EventLogLoggerProvider>((level) => level >= LogLevel.Warning);

        return builder;
    }

    public static ILoggingBuilder AddBravoDiagnosticLog(
        this ILoggingBuilder builder,
        DiagnosticLogStore store,
        DiagnosticLevelSwitch levelSwitch,
        TimeProvider timeProvider)
    {
        builder.AddDiagnosticLog(store, timeProvider);
        builder.AddFilter<DiagnosticLogProvider>((category, level) => level >= levelSwitch.MinimumLevel);

        // Framework records below Warning describe every request, the pane's own polling included.
        builder.AddFilter<DiagnosticLogProvider>("Microsoft", LogLevel.Warning);
        builder.AddFilter<DiagnosticLogProvider>("System", LogLevel.Warning);

        return builder;
    }
}
