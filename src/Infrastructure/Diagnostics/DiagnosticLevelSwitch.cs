using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Infrastructure.Configuration.Settings;

namespace Sqlbi.Bravo.Infrastructure.Diagnostics;

/// <summary>
/// Holds the diagnostic level chosen by the user, which sets the lowest level written to the diagnostics pane.
/// </summary>
/// <remarks>
/// The logger factory exists before the user settings are read, so the level is applied once they are loaded and
/// again whenever the user changes it.
/// </remarks>
public sealed class DiagnosticLevelSwitch(DiagnosticLevelType level = UserSettings.DefaultDiagnosticLevel)
{
    private volatile DiagnosticLevelType _level = level;

    public DiagnosticLevelType Level
    {
        get => _level;
        set => _level = value;
    }

    public LogLevel MinimumLevel => _level == DiagnosticLevelType.Verbose ? LogLevel.Information : LogLevel.Warning;
}
