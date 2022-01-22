using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;

namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    internal static class ExceptionHelper
    {
        public static void WriteToEventLog(Exception exception, EventLogEntryType type, bool throwOnError = true)
        {
            try
            {
                using var eventLog = new EventLog(logName: "Application", machineName: ".", source: "Application");
                var exceptionMessage = exception.ToString();
                eventLog.WriteEntry(exceptionMessage, type);
            }
            catch
            {
                if (throwOnError)
                    throw;
            }
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

        public static bool IsSafeException(Exception ex)
        {
            if (ex is not StackOverflowException && ex is not OutOfMemoryException && ex is not ThreadAbortException && ex is not AccessViolationException && ex is not SEHException)
            {
                return !typeof(SecurityException).IsAssignableFrom(ex.GetType());
            }

            return false;
        }
    }
}
