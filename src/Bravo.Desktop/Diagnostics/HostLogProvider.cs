using System;
using Bravo.Api.Diagnostics;
using Bravo.Platform.Windows.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Bravo.Desktop.Diagnostics;

/// <summary>
/// Writes every record of the process to the buffer that the diagnostics pane reads and to the file of the run.
/// </summary>
internal sealed class HostLogProvider(LogBuffer buffer, LogFile? file, TimeProvider timeProvider) : ILoggerProvider
{
    private readonly LogBuffer _buffer = buffer;
    private readonly LogFile? _file = file;
    private readonly TimeProvider _timeProvider = timeProvider;

    public ILogger CreateLogger(string categoryName)
    {
        return new HostLogger(categoryName, _buffer, _file, _timeProvider);
    }

    // The buffer and the file belong to the log, which closes the file after the last record.
    public void Dispose()
    {
    }
}
