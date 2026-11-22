using System;
using System.IO;
using System.Linq;
using Bravo.Platform.Windows.Diagnostics;
using Xunit;

namespace Bravo.Platform.Windows.Tests.Diagnostics;

public sealed class LogFileTests : IDisposable
{
    private static readonly DateTimeOffset s_start = new(2026, 11, 22, 10, 30, 15, TimeSpan.FromHours(1));

    private readonly TemporaryFolder _folder = new();

    public void Dispose()
    {
        _folder.Dispose();
    }

    [Fact]
    public void Create_Start_NamesTheFileAfterTheStartInUtcAndTheProcess()
    {
        using var file = LogFile.Create(_folder.Path, s_start, processId: 42);

        Assert.Equal(_folder.Combine("Bravo-20261122-093015-42.log"), file.Path);
    }

    [Fact]
    public void Create_FolderThatDoesNotExist_CreatesIt()
    {
        var folder = _folder.Combine("Logs");

        using var file = LogFile.Create(folder, s_start, processId: 42);

        Assert.True(File.Exists(file.Path));
    }

    [Fact]
    public void Create_FolderWithTheFilesOfEarlierRuns_KeepsTheNewestOnesWithTheNewFile()
    {
        for (var run = 1; run <= 4; run++)
        {
            File.WriteAllText(_folder.Combine($"Bravo-20261121-0{run}0000-1.log"), "An earlier run.");
        }

        using var file = LogFile.Create(_folder.Path, s_start, processId: 42, maxFiles: 3);

        var kept = Directory.GetFiles(_folder.Path).Select(Path.GetFileName).Order(StringComparer.Ordinal);
        Assert.Equal(["Bravo-20261121-030000-1.log", "Bravo-20261121-040000-1.log", "Bravo-20261122-093015-42.log"], kept);
    }

    [Fact]
    public void Write_Text_IsInTheFileWhileTheFileIsOpen()
    {
        using var file = LogFile.Create(_folder.Path, s_start, processId: 42);

        file.Write("A record.");

        Assert.Equal("A record.\r\n", ReadWhileOpen(file.Path));
    }

    [Fact]
    public void Write_BeyondTheLimit_SaysSoAndWritesNothingElse()
    {
        using var file = LogFile.Create(_folder.Path, s_start, processId: 42, maxBytes: 20);

        file.Write("Within the limit.");
        file.Write("Beyond the limit.");
        file.Write("After the limit.");

        var lines = ReadWhileOpen(file.Path).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.StartsWith("The file reached its limit", lines[1], StringComparison.Ordinal);
    }

    [Fact]
    public void Write_AfterTheFileWasClosed_DoesNothing()
    {
        var file = LogFile.Create(_folder.Path, s_start, processId: 42);
        file.Dispose();

        var exception = Record.Exception(() => file.Write("Too late."));

        Assert.Null(exception);
    }

    private static string ReadWhileOpen(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
