using Microsoft.Extensions.Logging;

namespace Sqlbi.Bravo.Infrastructure.Diagnostics;

/// <summary>
/// Writes log records to <see cref="AppEnvironment.Diagnostics"/>, the channel the user interface polls.
/// </summary>
[ProviderAlias("Diagnostics")]
internal sealed class DiagnosticMessageLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new DiagnosticMessageLogger(categoryName);

    public void Dispose()
    {
    }
}
