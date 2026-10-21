using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Sqlbi.Bravo.Models.ManageDates;

internal static partial class DateConfigurationExtensions
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Custom templates option read from the user settings ({ValueKind}): {Templates}")]
    private static partial void LogCustomTemplatesOptionRead(ILogger logger, JsonValueKind valueKind, JsonElement templates);
}
