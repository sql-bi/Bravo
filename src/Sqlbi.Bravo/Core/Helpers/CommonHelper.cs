using Humanizer;
using Humanizer.Localisation;
using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;

namespace Sqlbi.Bravo.Core.Helpers
{
    internal static class CommonHelper
    {
        public static string HumanizeElapsed(this DateTime dateTime)
        {
            if (dateTime == DateTime.MinValue)
            {
                return "not yet";
            }

            var elapsed = DateTime.UtcNow.Subtract(dateTime);
            var elapsedHumanized = elapsed.Humanize(minUnit: TimeUnit.Second, culture: AppConstants.ApplicationDefaultCulture);
            var elapsedText = $"{ elapsedHumanized.Replace("minute", "min").Replace("second", "sec") } ago";

            return elapsedText;
        }

        public static string ToPowerBIDesktopReportName(this string windowTitle)
        {
            var index = windowTitle.LastIndexOf(" - Power BI Desktop");
            if (index >= 0)
            {
                return windowTitle.Substring(0, index);
            }

            return windowTitle;
        }

        public static bool IsSafeException(this Exception exception)
        {
            var isAccessViolationException = exception is AccessViolationException;
            var isStackOverflowException = exception is StackOverflowException;
            var isThreadAbortException = exception is ThreadAbortException;
            var isOutOfMemoryException = exception is OutOfMemoryException;
            var isSEHException = exception is SEHException;

            if (!isAccessViolationException && !isStackOverflowException && !isThreadAbortException && !isOutOfMemoryException && !isSEHException)
            {
                var isSecurityException = typeof(SecurityException).IsAssignableFrom(exception.GetType());

                return !isSecurityException;
            }

            return false;
        }
    }
}