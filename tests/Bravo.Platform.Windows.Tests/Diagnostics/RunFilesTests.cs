using System;
using System.IO;
using System.Linq;
using Bravo.Platform.Windows.Diagnostics;
using Xunit;

namespace Bravo.Platform.Windows.Tests.Diagnostics;

public sealed class RunFilesTests : IDisposable
{
    private readonly TemporaryFolder _folder = new();

    public void Dispose()
    {
        _folder.Dispose();
    }

    [Fact]
    public void GetName_Start_NamesTheRunByItsStartInUtcAndItsProcess()
    {
        var start = new DateTimeOffset(2026, 11, 22, 10, 30, 15, TimeSpan.FromHours(1));

        Assert.Equal("Bravo-20261122-093015-42.crash.json", RunFiles.GetName(start, processId: 42, ".crash.json"));
    }

    // The reports of the crashes and the files of the log share a folder, each kind with a limit of its own.
    [Fact]
    public void MakeRoom_FilesOfAnotherKind_AreNeitherDeletedNorCounted()
    {
        string[] names =
        [
            "Bravo-20261121-010000-1.log",
            "Bravo-20261121-010000-1.crash.json",
            "Bravo-20261121-020000-1.log",
            "Bravo-20261121-030000-1.log",
        ];
        foreach (var name in names)
        {
            File.WriteAllText(_folder.Combine(name), "An earlier run.");
        }

        RunFiles.MakeRoom(_folder.Path, ".log", maxFiles: 3);

        var kept = Directory.GetFiles(_folder.Path).Select(Path.GetFileName).Order(StringComparer.Ordinal);
        Assert.Equal(
            ["Bravo-20261121-010000-1.crash.json", "Bravo-20261121-020000-1.log", "Bravo-20261121-030000-1.log"],
            kept);
    }

    // The first run, or a crash before the log was open: the folder is not there yet.
    [Fact]
    public void MakeRoom_FolderThatIsNotThere_CreatesIt()
    {
        var folder = _folder.Combine("Logs");

        RunFiles.MakeRoom(folder, ".crash.json", maxFiles: 10);

        Assert.True(Directory.Exists(folder));
    }
}
