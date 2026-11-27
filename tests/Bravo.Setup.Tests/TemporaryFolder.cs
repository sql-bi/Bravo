using System;
using System.IO;

namespace Bravo.Setup.Tests
{
    /// <summary>
    /// A folder of one test, under the temporary folder of the user, removed with everything in it when the
    /// test ends. The library under test never gets another place to write in.
    /// </summary>
    internal sealed class TemporaryFolder : IDisposable
    {
        public TemporaryFolder()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Bravo.Setup.Tests.{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string Combine(params string[] names)
        {
            var path = Path;
            foreach (var name in names)
            {
                path = System.IO.Path.Combine(path, name);
            }

            return path;
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
