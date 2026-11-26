using Bravo.Desktop.Development;

namespace Bravo.Desktop.Diagnostics;

/// <summary>
/// Starts the reporter: the application again, in a process of its own, to show the report of a crash.
/// </summary>
internal interface ICrashReporterLauncher
{
    /// <summary>
    /// Starts the reporter and returns at once.
    /// </summary>
    /// <param name="reportPath">The file of the report.</param>
    /// <param name="development">The options of a development build that the reporter is started with.</param>
    /// <returns>Whether the reporter started.</returns>
    bool TryStart(string reportPath, DevelopmentOptions development);
}
