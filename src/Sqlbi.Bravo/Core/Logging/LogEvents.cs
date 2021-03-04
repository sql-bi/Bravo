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

            MediaPlaybackFailed = 9012,

            MediaPlaybackOpenHyperlink = 9013,

            NavigateHyperlink = 9014,

            ConnectionTypeVertipaqFile = 9015,

            ConnectionTypePowerBIDataset = 9016,

            ConnectionTypeAttachPowerBI = 9017
        }

        public static readonly EventId TaskSchedulerUnobservedTaskException = new EventId((int)LogEventsId.TaskSchedulerUnobservedTaskException);

        public static readonly EventId AppDomainUnhandledException = new EventId((int)LogEventsId.AppDomainUnhandledException);

        public static readonly EventId DispatcherUnhandledException = new EventId((int)LogEventsId.DispatcherUnhandledException);

        public static readonly EventId AppOnStartup = new EventId((int)LogEventsId.AppOnStartup);

        public static readonly EventId AppOnExit = new EventId((int)LogEventsId.AppOnExit);

        public static readonly EventId AppShutdownForMultipleInstance = new EventId((int)LogEventsId.AppShutdownForMultipleInstance);

        public static readonly EventId DaxFormatterApplyFormatContainsErrors = new EventId((int)LogEventsId.DaxFormatterApplyFormatContainsErrors);

        public static readonly EventId ApplicationInstanceServiceMultipleInstanceTaskException = new EventId((int)LogEventsId.ApplicationInstanceServiceMultipleInstanceTaskException);

        public static readonly EventId AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskException = new EventId((int)LogEventsId.AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskException);

        public static readonly EventId AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskCompleted = new EventId((int)LogEventsId.AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskCompleted);

        public static readonly EventId AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskStatus = new EventId((int)LogEventsId.AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskStatus);

        public static readonly EventId AnalysisServicesEventWatcherServiceOnTraceEvent = new EventId((int)LogEventsId.AnalysisServicesEventWatcherServiceOnTraceEvent);

        public static readonly EventId MediaPlaybackFailed = new EventId((int)LogEventsId.MediaPlaybackFailed);

        public static readonly EventId MediaPlaybackOpenHyperlink = new EventId((int)LogEventsId.MediaPlaybackOpenHyperlink);

        public static readonly EventId NavigateHyperlink = new EventId((int)LogEventsId.NavigateHyperlink);

        public static readonly EventId ConnectionTypeVertipaqFile = new EventId((int)LogEventsId.ConnectionTypeVertipaqFile);

        public static readonly EventId ConnectionTypePowerBIDataset = new EventId((int)LogEventsId.ConnectionTypePowerBIDataset);

        public static readonly EventId ConnectionTypeAttachPowerBI = new EventId((int)LogEventsId.ConnectionTypeAttachPowerBI);
    }
}
