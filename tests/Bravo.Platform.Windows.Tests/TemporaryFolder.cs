using System;
using System.IO;

namespace Bravo.Platform.Windows.Tests;

/// <summary>
/// A folder of one test, under the temporary folder of the user, removed with everything in it when the test
/// ends: no test reads or writes the data folder of the user.
/// </summary>
internal sealed class TemporaryFolder : IDisposable
{
    public TemporaryFolder()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Bravo.Tests.{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string Combine(params string[] names)
    {
        return System.IO.Path.Combine([Path, .. names]);
    }

    public void Dispose()
    {
        Directory.Delete(Path, recursive: true);
    }
}
