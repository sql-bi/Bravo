using Microsoft.Extensions.Logging;

namespace Sqlbi.Bravo.Core.Logging
{
    internal static class LogEvents
    {
        private enum LogEventsId: int
        {
            TaskSchedulerUnobservedTaskException = 9000,

            AppDomainUnhandledException = 9001,

            DispatcherUnhandledException = 9002,

            AppOnStartup = 9003,

            AppOnExit = 9004,

            AppShutdownForMultipleInstance = 9005,

            DaxFormatterApplyFormatContainsErrors = 9006,

            ApplicationInstanceServiceMultipleInstanceTaskException = 9007,

            AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskException = 9008,

            AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskCompleted = 9009,

            AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskStatus = 9010,

            AnalysisServicesEventWatcherServiceOnTraceEvent = 9011,
        }

        public static readonly EventId TaskSchedulerUnobservedTaskException = new EventId((int)LogEventsId.TaskSchedulerUnobservedTaskException, nameof(TaskSchedulerUnobservedTaskException));

        public static readonly EventId AppDomainUnhandledException = new EventId((int)LogEventsId.AppDomainUnhandledException, nameof(AppDomainUnhandledException));

        public static readonly EventId DispatcherUnhandledException = new EventId((int)LogEventsId.DispatcherUnhandledException, nameof(DispatcherUnhandledException));

        public static readonly EventId AppOnStartup = new EventId((int)LogEventsId.AppOnStartup, nameof(AppOnStartup));

        public static readonly EventId AppOnExit = new EventId((int)LogEventsId.AppOnExit, nameof(AppOnExit));

        public static readonly EventId AppShutdownForMultipleInstance = new EventId((int)LogEventsId.AppShutdownForMultipleInstance, nameof(AppShutdownForMultipleInstance));

        public static readonly EventId DaxFormatterApplyFormatContainsErrors = new EventId((int)LogEventsId.DaxFormatterApplyFormatContainsErrors, nameof(DaxFormatterApplyFormatContainsErrors));

        public static readonly EventId ApplicationInstanceServiceMultipleInstanceTaskException = new EventId((int)LogEventsId.ApplicationInstanceServiceMultipleInstanceTaskException, nameof(ApplicationInstanceServiceMultipleInstanceTaskException));

        public static readonly EventId AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskException = new EventId((int)LogEventsId.AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskException, nameof(AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskException));

        public static readonly EventId AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskCompleted = new EventId((int)LogEventsId.AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskCompleted, nameof(AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskCompleted));

        public static readonly EventId AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskStatus = new EventId((int)LogEventsId.AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskStatus, nameof(AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskStatus));

        public static readonly EventId AnalysisServicesEventWatcherServiceOnTraceEvent = new EventId((int)LogEventsId.AnalysisServicesEventWatcherServiceOnTraceEvent, nameof(AnalysisServicesEventWatcherServiceOnTraceEvent));
    }
}
