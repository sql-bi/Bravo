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

        public static bool IsPBIMainWindowTitle(this string windowTitle)
        {
            return windowTitle.EndsWith(AppConstants.PBIDesktopMainWindowTitleSuffix);
        }
    }
}
