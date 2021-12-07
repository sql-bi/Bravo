namespace Sqlbi.Bravo.Infrastructure.Extensions
{
    public static class StringExtensions
    {
        public static string ToPBIDesktopReportName(this string windowTitle)
        {
            var index = windowTitle.LastIndexOf(" - Power BI Desktop");
            if (index >= 0)
                return windowTitle[..index];

            return windowTitle;
        }
    }
}
