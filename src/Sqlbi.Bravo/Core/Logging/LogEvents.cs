using Microsoft.Extensions.Logging;

namespace Sqlbi.Bravo.Core.Logging
{
    internal static class LogEvents
    {
        private enum LogEventsId: int
        {
            DaxFormatterUnableToRetrievePowerBIDesktopFileNameFromParentProcessMainWindowTitle = 9000, // TODO REQUIREMENTS: start value

            DaxFormatterHttpClientFormatResponse,

            DaxFormatterHttpClientFormatError,

            DaxFormatterHttpClientUriChanged,

            TaskSchedulerUnobservedTaskException,

            AppDomainUnhandledException,

            DispatcherUnhandledException,

            AppOnStartup,

            AppOnExit,

            AppShutdownForMultipleInstance,

            DaxFormatterModelManagerBuildRequestFromModel,

            DaxFormatterModelManagerCreateForModel,

            DaxFormatterFormatSaveChangesContainsErrors,

            ApplicationInstanceServiceMultipleInstanceTaskException,

            AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskException,

            AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskCompleted,

            AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskStatus,

            AnalysisServicesEventWatcherServiceOnTraceEvent,
        }

        public static readonly EventId DaxFormatterUnableToRetrievePowerBIDesktopFileNameFromParentProcessMainWindowTitle = new EventId((int)LogEventsId.DaxFormatterUnableToRetrievePowerBIDesktopFileNameFromParentProcessMainWindowTitle, nameof(DaxFormatterUnableToRetrievePowerBIDesktopFileNameFromParentProcessMainWindowTitle));

        public static readonly EventId DaxFormatterHttpClientFormatResponse = new EventId((int)LogEventsId.DaxFormatterHttpClientFormatResponse, nameof(DaxFormatterHttpClientFormatResponse));

        public static readonly EventId DaxFormatterHttpClientFormatError = new EventId((int)LogEventsId.DaxFormatterHttpClientFormatError, nameof(DaxFormatterHttpClientFormatError));

        public static readonly EventId DaxFormatterHttpClientUriChanged = new EventId((int)LogEventsId.DaxFormatterHttpClientUriChanged, nameof(DaxFormatterHttpClientUriChanged));

        public static readonly EventId TaskSchedulerUnobservedTaskException = new EventId((int)LogEventsId.TaskSchedulerUnobservedTaskException, nameof(TaskSchedulerUnobservedTaskException));

        public static readonly EventId AppDomainUnhandledException = new EventId((int)LogEventsId.AppDomainUnhandledException, nameof(AppDomainUnhandledException));

        public static readonly EventId DispatcherUnhandledException = new EventId((int)LogEventsId.DispatcherUnhandledException, nameof(DispatcherUnhandledException));

        public static readonly EventId AppOnStartup = new EventId((int)LogEventsId.AppOnStartup, nameof(AppOnStartup));

        public static readonly EventId AppOnExit = new EventId((int)LogEventsId.AppOnExit, nameof(AppOnExit));

        public static readonly EventId AppShutdownForMultipleInstance = new EventId((int)LogEventsId.AppShutdownForMultipleInstance, nameof(AppShutdownForMultipleInstance));

        public static readonly EventId DaxFormatterModelManagerBuildRequestFromModel = new EventId((int)LogEventsId.DaxFormatterModelManagerBuildRequestFromModel, nameof(DaxFormatterModelManagerBuildRequestFromModel));

        public static readonly EventId DaxFormatterModelManagerCreateForModel = new EventId((int)LogEventsId.DaxFormatterModelManagerCreateForModel, nameof(DaxFormatterModelManagerCreateForModel));

        public static readonly EventId DaxFormatterFormatSaveChangesContainsErrors = new EventId((int)LogEventsId.DaxFormatterFormatSaveChangesContainsErrors, nameof(DaxFormatterFormatSaveChangesContainsErrors));

        public static readonly EventId ApplicationInstanceServiceMultipleInstanceTaskException = new EventId((int)LogEventsId.ApplicationInstanceServiceMultipleInstanceTaskException, nameof(ApplicationInstanceServiceMultipleInstanceTaskException));

        public static readonly EventId AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskException = new EventId((int)LogEventsId.AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskException, nameof(AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskException));

        public static readonly EventId AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskCompleted = new EventId((int)LogEventsId.AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskCompleted, nameof(AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskCompleted));

        public static readonly EventId AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskStatus = new EventId((int)LogEventsId.AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskStatus, nameof(AnalysisServicesEventWatcherServiceConnectionStateMonitorTaskStatus));

        public static readonly EventId AnalysisServicesEventWatcherServiceOnTraceEvent = new EventId((int)LogEventsId.AnalysisServicesEventWatcherServiceOnTraceEvent, nameof(AnalysisServicesEventWatcherServiceOnTraceEvent));
    }
}
