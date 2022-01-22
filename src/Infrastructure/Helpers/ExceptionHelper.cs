using System;
using System.Diagnostics;

namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    internal static class ExceptionHelper
    {
        public static void WriteEventLogWarning(Exception exception)
        {
            using var eventLog = new EventLog(logName: "Application", machineName: ".", source: "Application");
            var exceptionMessage = exception.ToString();
            eventLog.WriteEntry(exceptionMessage, EventLogEntryType.Warning);
        }

        public static bool IsOrHasInner<T>(this Exception exception) where T : Exception
        {
            var foundException = Find<T>(exception);
            return foundException != null;
        }

        public static T? Find<T>(this Exception exception) where T : Exception
        {
            if (exception is T foundException)
                return foundException;

            var innerException = exception.InnerException;

            while (innerException is not null)
            {
                if (innerException is T foundInnerException)
                    return foundInnerException;

                innerException = innerException.InnerException;
            }

            return null;
        }
    }
}
