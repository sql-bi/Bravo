using Microsoft.Extensions.Logging;

namespace Bravo.Architecture.Tests.StaticState.Fixtures;

internal sealed partial class ModelCounter
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Counted {Count} models with a long name")]
    private partial void LogCounted(int count);
}
