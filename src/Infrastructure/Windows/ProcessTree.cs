using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Sqlbi.Bravo.Infrastructure.Windows.Interop;

namespace Sqlbi.Bravo.Infrastructure.Windows;

internal readonly record struct ProcessEntry(int ProcessId, int ParentProcessId, string ImageName);

/// <summary>
/// The processes that were running at one moment, each with the identifier of the process that created it.
/// </summary>
/// <remarks>
/// A parent identifier is recorded when the process is created and is not updated: once the parent exits, the
/// identifier can belong to an unrelated process.
/// </remarks>
internal sealed class ProcessTree
{
    private readonly Dictionary<int, ProcessEntry> _entries = [];

    public ProcessTree(IEnumerable<ProcessEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        foreach (var entry in entries)
            _ = _entries.TryAdd(entry.ProcessId, entry);
    }

    /// <summary>
    /// Takes a snapshot of the processes running now.
    /// </summary>
    /// <exception cref="Win32Exception">The processes cannot be read.</exception>
    public static ProcessTree Capture()
    {
        var entries = ProcessInterop.GetProcesses();

        return new ProcessTree(entries);
    }

    /// <summary>
    /// Returns the identifier of the process that created <paramref name="processId"/>; <see langword="null"/> when the
    /// process is not in the snapshot.
    /// </summary>
    public int? GetParentId(int processId)
    {
        if (!_entries.TryGetValue(processId, out var entry))
            return null;

        return entry.ParentProcessId;
    }

    /// <summary>
    /// Returns the processes created by a process, optionally only those with the given image name.
    /// </summary>
    /// <param name="processId">The identifier of the parent process.</param>
    /// <param name="imageName">The executable file name, such as <c>msmdsrv.exe</c>, compared without case.</param>
    public IReadOnlyList<int> GetChildIds(int processId, string? imageName = null)
    {
        return _entries.Values
            .Where((entry) => entry.ParentProcessId == processId && entry.ProcessId != processId)
            .Where((entry) => imageName is null || string.Equals(entry.ImageName, imageName, StringComparison.OrdinalIgnoreCase))
            .Select((entry) => entry.ProcessId)
            .ToArray();
    }
}
