using System;
using Microsoft.Extensions.Logging;

namespace Sqlbi.Bravo.Host;

internal sealed partial class BootstrapContextFactory
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Telemetry could not be created. Telemetry is disabled for this session.")]
    private static partial void LogTelemetryCreationFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "User settings could not be created. Default settings will be used for this session.")]
    private static partial void LogUserSettingsCreationFailed(ILogger logger, Exception exception);
}
