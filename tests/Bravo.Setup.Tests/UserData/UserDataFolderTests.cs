using System;
using System.IO;
using Bravo.Setup.UserData;
using Xunit;

namespace Bravo.Setup.Tests.UserData;

public sealed class UserDataFolderTests : IDisposable
{
    private readonly TemporaryFolder _folder = new();
    private readonly UserDataFolder _userData = new();

    public void Dispose()
    {
        _folder.Dispose();
    }

    [Fact]
    public void Remove_AFolderWithFilesAndFolders_RemovesItAndLeavesWhatIsBesideIt()
    {
        var dataFolder = _folder.Combine("Bravo");
        Directory.CreateDirectory(Path.Combine(dataFolder, "WebView2", "EBWebView"));
        File.WriteAllText(Path.Combine(dataFolder, "usersettings.json"), "{}");
        File.WriteAllText(Path.Combine(dataFolder, "WebView2", "EBWebView", "Local State"), "{}");
        Directory.CreateDirectory(_folder.Combine("Bravo.Development"));

        _userData.Remove(dataFolder);

        Assert.Equal([_folder.Combine("Bravo.Development")], Directory.GetFileSystemEntries(_folder.Path));
    }

    [Fact]
    public void Remove_AFolderThatIsNotThere_DoesNothing()
    {
        _userData.Remove(_folder.Combine("Bravo"));

        Assert.Empty(Directory.GetFileSystemEntries(_folder.Path));
    }

    // A file that a program holds open cannot be removed: the caller is told, and the folder stays.
    [Fact]
    public void Remove_AFolderWithAFileInUse_Throws()
    {
        var dataFolder = _folder.Combine("Bravo");
        Directory.CreateDirectory(dataFolder);
        var lockedFile = Path.Combine(dataFolder, "lock");
        using (new FileStream(lockedFile, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            Assert.Throws<IOException>(() => _userData.Remove(dataFolder));
        }

        Assert.True(File.Exists(lockedFile));
    }
}
