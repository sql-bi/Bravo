using System;
using System.Collections.Immutable;

namespace Sqlbi.Bravo.Infrastructure.Services.PowerBI;

internal static class PowerBIDesktop
{
    public const string ProcessName = "PBIDesktop";

    public const string AnalysisServicesImageName = "msmdsrv.exe";

    // The main window title is culture-specific. The suffix is not always present, for example, it is not added when
    // the save/share function in OneDrive/SharePoint is active.
    private static readonly ImmutableArray<string> s_applicationNameSuffixes =
    [
        // Different dash characters are used as a separator
        // See https://github.com/sql-bi/Bravo/issues/476
        " \u002D Power BI Desktop", // Dash Punctuation - minus hyphen
        " \u2013 Power BI Desktop", // Dash Punctuation - en dash
        " \u2014 Power BI Desktop", // Dash Punctuation - em dash

        // The whitespace character may not be present - Swedish/sv
        // See https://github.com/sql-bi/Bravo/issues/510
        "\u2013Power BI Desktop", // Dash Punctuation - en dash

        // NBSP char instead of whitespace - Latvian/lv
        "\u00A0\u2014 Power BI Desktop",
    ];

    /// <summary>
    /// Returns the report name shown in the main window title of Power BI Desktop.
    /// </summary>
    /// <returns>
    /// The title without the application name; <see langword="null"/> when the title is empty, which happens while
    /// Power BI Desktop starts its Analysis Services instance or loads the model.
    /// </returns>
    public static string? GetReportName(string windowTitle)
    {
        ArgumentNullException.ThrowIfNull(windowTitle);

        if (string.IsNullOrWhiteSpace(windowTitle))
            return null;

        foreach (var suffix in s_applicationNameSuffixes)
        {
            var index = windowTitle.LastIndexOf(suffix, StringComparison.Ordinal);
            if (index >= 0)
                return windowTitle[..index];
        }

        return windowTitle;
    }
}
