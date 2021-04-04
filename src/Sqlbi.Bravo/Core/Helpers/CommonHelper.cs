using Humanizer;
using Humanizer.Localisation;
using System;

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
    }
}
