using System.IO;

namespace Bravo.Platform.Windows.Tests;

/// <summary>
/// Reads a file that is still open for writing, as the file of a log is while its run goes on. The tests of
/// <c>Bravo.Desktop</c> compile the same file.
/// </summary>
internal static class OpenFile
{
    public static string ReadAllText(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
