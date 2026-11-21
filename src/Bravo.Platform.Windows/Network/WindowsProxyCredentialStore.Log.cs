using System;
using Microsoft.Extensions.Logging;

namespace Bravo.Platform.Windows.Network;

public sealed partial class WindowsProxyCredentialStore
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "The proxy credentials '{TargetName}' cannot be read: the proxy server is answered without them.")]
    private partial void LogReadFailed(Exception exception, string targetName);
}
