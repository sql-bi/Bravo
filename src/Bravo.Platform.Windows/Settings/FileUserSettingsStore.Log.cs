using System;
using Microsoft.Extensions.Logging;

namespace Bravo.Platform.Windows.Settings;

public sealed partial class FileUserSettingsStore
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "The settings file '{Path}' cannot be read: the settings are the defaults.")]
    private partial void LogReadFailed(Exception exception, string path);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "The settings file '{Path}' cannot be written: the settings are unchanged.")]
    private partial void LogSaveFailed(Exception exception, string path);
}
