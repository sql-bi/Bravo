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

            DaxFormatterException = 9006,

            ApplicationInstanceServiceException = 9007,

            AnalysisServicesEventWatcherServiceException = 9008,

            AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskCompleted = 9009,

            AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskStatus = 9010,

            AnalysisServicesEventWatcherServiceOnTraceEvent = 9011,

            MediaDialogExcetpion = 9012,

            MediaDialogViewAction = 9013,

            NavigateHyperlink = 9014,

            StartConnectionAction = 9015,

            ShellViewException = 9016,

            DaxFormatterViewAction = 9017,

            AnalyzeModelViewAction = 9018,

            AnalyzeModelException = 9019,

            ShellViewAction = 9020,
        }

        public static readonly EventId TaskSchedulerUnobservedTaskException = new EventId((int)LogEventsId.TaskSchedulerUnobservedTaskException, nameof(LogEventsId.TaskSchedulerUnobservedTaskException));

        public static readonly EventId AppDomainUnhandledException = new EventId((int)LogEventsId.AppDomainUnhandledException, nameof(LogEventsId.AppDomainUnhandledException));

        public static readonly EventId DispatcherUnhandledException = new EventId((int)LogEventsId.DispatcherUnhandledException, nameof(LogEventsId.DispatcherUnhandledException));

        public static readonly EventId AppOnStartup = new EventId((int)LogEventsId.AppOnStartup, nameof(LogEventsId.AppOnStartup));

        public static readonly EventId AppOnExit = new EventId((int)LogEventsId.AppOnExit, nameof(LogEventsId.AppOnExit));

        public static readonly EventId DaxFormatterException = new EventId((int)LogEventsId.DaxFormatterException, nameof(LogEventsId.DaxFormatterException));

        public static readonly EventId ApplicationInstanceServiceException = new EventId((int)LogEventsId.ApplicationInstanceServiceException, nameof(LogEventsId.ApplicationInstanceServiceException));

        public static readonly EventId AnalysisServicesEventWatcherServiceException = new EventId((int)LogEventsId.AnalysisServicesEventWatcherServiceException, nameof(LogEventsId.AnalysisServicesEventWatcherServiceException));

        public static readonly EventId AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskCompleted = new EventId((int)LogEventsId.AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskCompleted, nameof(LogEventsId.AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskCompleted));

        public static readonly EventId AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskStatus = new EventId((int)LogEventsId.AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskStatus, nameof(LogEventsId.AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskStatus));

        public static readonly EventId AnalysisServicesEventWatcherServiceOnTraceEvent = new EventId((int)LogEventsId.AnalysisServicesEventWatcherServiceOnTraceEvent, nameof(LogEventsId.AnalysisServicesEventWatcherServiceOnTraceEvent));

        public static readonly EventId MediaDialogExcetpion = new EventId((int)LogEventsId.MediaDialogExcetpion, nameof(LogEventsId.MediaDialogExcetpion));

        public static readonly EventId MediaDialogViewAction = new EventId((int)LogEventsId.MediaDialogViewAction, nameof(LogEventsId.MediaDialogViewAction));

        public static readonly EventId NavigateHyperlink = new EventId((int)LogEventsId.NavigateHyperlink, nameof(LogEventsId.NavigateHyperlink));

        public static readonly EventId StartConnectionAction = new EventId((int)LogEventsId.StartConnectionAction, nameof(LogEventsId.StartConnectionAction));

        public static readonly EventId ShellViewException = new EventId((int)LogEventsId.ShellViewException, nameof(LogEventsId.ShellViewException));

        public static readonly EventId DaxFormatterViewAction = new EventId((int)LogEventsId.DaxFormatterViewAction, nameof(LogEventsId.DaxFormatterViewAction));

        public static readonly EventId AnalyzeModelViewAction = new EventId((int)LogEventsId.AnalyzeModelViewAction, nameof(LogEventsId.AnalyzeModelViewAction));

        public static readonly EventId AnalyzeModelException = new EventId((int)LogEventsId.AnalyzeModelException, nameof(LogEventsId.AnalyzeModelException));

        public static readonly EventId ShellViewAction = new EventId((int)LogEventsId.ShellViewAction, nameof(LogEventsId.ShellViewAction));
    }
}
