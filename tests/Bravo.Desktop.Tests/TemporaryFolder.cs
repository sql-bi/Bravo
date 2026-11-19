using System;
using System.IO;

namespace Bravo.Desktop.Tests;

/// <summary>
/// A folder of one test, under the temporary folder of the user, removed with everything in it when the test
/// ends: the data folder of a host that a test builds, so that no test reads or writes the one of the user.
/// </summary>
internal sealed class TemporaryFolder : IDisposable
{
    public TemporaryFolder()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Bravo.Tests.{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public DataFolder AsDataFolder()
    {
        return new DataFolder(Path);
    }

    public string Combine(params string[] names)
    {
        return System.IO.Path.Combine([Path, .. names]);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // The engine of a webview may still hold files of its data folder for a moment after it ended.
        }
    }
}
