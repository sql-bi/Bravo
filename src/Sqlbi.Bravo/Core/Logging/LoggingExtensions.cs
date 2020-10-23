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

        public static void Trace(this Microsoft.Extensions.Logging.ILogger logger, string message = null, [CallerMemberName] string callerMemberName = null, params object[] args)
        {
            using var property = LogContext.PushProperty("CallerMemberName", callerMemberName);
            logger.LogTrace(message, args);
        }

        public static void Warning(this Microsoft.Extensions.Logging.ILogger logger, EventId eventId, string message = null, [CallerMemberName] string callerMemberName = null, params object[] args)
        {
            using var property = LogContext.PushProperty("CallerMemberName", callerMemberName);
            logger.LogWarning(eventId, message, args);
        }

        public static void Debug(this Microsoft.Extensions.Logging.ILogger logger, EventId eventId, string message = null, [CallerMemberName] string callerMemberName = null, params object[] args)
        {
            using var property = LogContext.PushProperty("CallerMemberName", callerMemberName);
            logger.LogDebug(eventId, message, args);
        }

        public static void Error(this Microsoft.Extensions.Logging.ILogger logger, EventId eventId, Exception exception, string message = null, [CallerMemberName] string callerMemberName = null, params object[] args)
        {
            using var property = LogContext.PushProperty("CallerMemberName", callerMemberName);
            logger.LogError(eventId, exception, message, args);
        }

        public static void Error(this Microsoft.Extensions.Logging.ILogger logger, EventId eventId, string message = null, [CallerMemberName] string callerMemberName = null, params object[] args)
        {
            using var property = LogContext.PushProperty("CallerMemberName", callerMemberName);
            logger.LogError(eventId, message, args);
        }

        public static void Information(this Microsoft.Extensions.Logging.ILogger logger, EventId eventId, string message = null, [CallerMemberName] string callerMemberName = null, params object[] args)
        {
            using var property = LogContext.PushProperty("CallerMemberName", callerMemberName);
            logger.LogInformation(eventId, message, args);
        }
    }
}
