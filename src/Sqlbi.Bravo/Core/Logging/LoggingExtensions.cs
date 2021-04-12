using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Configuration;
using Serilog.Context;
using System;
using System.Runtime.CompilerServices;

namespace Sqlbi.Bravo.Core.Logging
{
    internal static class LoggingExtensions
    {
        public static LoggerConfiguration WithManagedThread(this LoggerEnrichmentConfiguration configuration) => configuration.With<ManagedThreadEnricher>();

        public static void Trace(this Microsoft.Extensions.Logging.ILogger logger, string message = null, object[] args = null, [CallerMemberName] string callerMemberName = null)
        {
            using var property = LogContext.PushProperty("CallerMemberName", callerMemberName);
            logger.LogTrace(message, args);
        }

        public static void Debug(this Microsoft.Extensions.Logging.ILogger logger, EventId eventId, string message = null, object[] args = null, [CallerMemberName] string callerMemberName = null)
        {
            using var property = LogContext.PushProperty("CallerMemberName", callerMemberName);
            message = FormatMessage(eventId, message);
            logger.LogDebug(eventId, message, args);
        }

        public static void Information(this Microsoft.Extensions.Logging.ILogger logger, EventId eventId, string message = null, object[] args = null, [CallerMemberName] string callerMemberName = null)
        {
            using var property = LogContext.PushProperty("CallerMemberName", callerMemberName);
            message = FormatMessage(eventId, message);
            logger.LogInformation(eventId, message, args);
        }

        public static void Error(this Microsoft.Extensions.Logging.ILogger logger, EventId eventId, Exception exception, string message = null, object[] args = null, [CallerMemberName] string callerMemberName = null)
        {
            using var property = LogContext.PushProperty("CallerMemberName", callerMemberName);
            message = FormatMessage(eventId, message);
            logger.LogError(eventId, exception, message, args);
        }

        private static string FormatMessage(EventId eventId, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return eventId.Name;

            return $"{ eventId.Name } - { message }";
        }
    }
}
