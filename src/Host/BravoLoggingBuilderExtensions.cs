using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;

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
}
