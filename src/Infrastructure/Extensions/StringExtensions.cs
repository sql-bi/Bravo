using System;
using System.Text.RegularExpressions;

namespace Sqlbi.Bravo.Infrastructure.Extensions
{
    public static class StringExtensions
    {
        public static string ToPBIDesktopReportName(this string windowTitle)
        {
            var index = windowTitle.LastIndexOf(AppConstants.PBIDesktopMainWindowTitleSuffix);
            if (index >= 0)
                return windowTitle[..index];

            return windowTitle;
        }

        public static bool IsPBIDesktopMainWindowTitle(this string windowTitle)
        {
            return windowTitle.EndsWith(AppConstants.PBIDesktopMainWindowTitleSuffix);
        }

        /// <summary>
        /// Convert the old Microsoft datetime offset "/Date(1617810719887)/" to <see cref="DateTimeOffset"/>
        /// </summary>
        /// <param name="microsoftDateTimeOffset">Old Microsoft datetime offset string "/Date(1617810719887)/"</param>
        public static DateTimeOffset? ToDateTimeOffset(this string microsoftDateTimeOffset)
        {
            var regex = new Regex("^\\/Date\\(([0-9]+)\\)\\/$");

            var match = regex.Match(microsoftDateTimeOffset);
            if (match.Success)
            {
                var seconds = long.Parse(match.Groups[1].Value);
                return DateTimeOffset.FromUnixTimeMilliseconds(seconds);
            }

            return null;
        }
    }
}
