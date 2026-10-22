using System;
using System.IO;

namespace Bravo.Tests;

/// <summary>
/// A folder under the temporary path, unique to one test and deleted when disposed.
/// </summary>
internal sealed class TemporaryFolder : IDisposable
{
    public string Path { get; } = Directory.CreateDirectory(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Bravo.Tests.{Guid.NewGuid():N}")).FullName;

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // A file still held open, for example by a telemetry channel, is left behind.
        }
    }
}
