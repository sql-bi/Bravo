using System;
using System.IO;
using Bravo.Setup.UserData;
using Xunit;

namespace Bravo.Setup.Tests.UserData;

/// <summary>
/// The folder of the test stands for the local application data of a user.
/// </summary>
public sealed class UserDataFolderTests : IDisposable
{
    private readonly TemporaryFolder _folder = new();
    private readonly UserDataFolder _userData = new();

    public void Dispose()
    {
        _folder.Dispose();
    }

    [Fact]
    public void Remove_RemovesTheDataFolderOfTheApplicationAndNothingElse()
    {
        Directory.CreateDirectory(_folder.Combine("Bravo", "WebView2", "EBWebView"));
        File.WriteAllText(_folder.Combine("Bravo", "usersettings.json"), "{}");
        File.WriteAllText(_folder.Combine("Bravo", "WebView2", "EBWebView", "Local State"), "{}");
        Directory.CreateDirectory(_folder.Combine("Bravo.Development"));
        File.WriteAllText(_folder.Combine("another-application.txt"), "");

        _userData.Remove(_folder.Path);

        string[] left = [_folder.Combine("another-application.txt"), _folder.Combine("Bravo.Development")];
        Assert.Equal(left, Directory.GetFileSystemEntries(_folder.Path));
    }

    // The folder of Windows Installer ends with a backslash.
    [Fact]
    public void Remove_AFolderThatEndsWithABackslash_RemovesTheDataFolder()
    {
        Directory.CreateDirectory(_folder.Combine("Bravo"));

        _userData.Remove(_folder.Path + @"\");

        Assert.Empty(Directory.GetFileSystemEntries(_folder.Path));
    }

    [Fact]
    public void Remove_NoDataFolder_DoesNothing()
    {
        File.WriteAllText(_folder.Combine("another-application.txt"), "");

        _userData.Remove(_folder.Path);

        Assert.Equal([_folder.Combine("another-application.txt")], Directory.GetFileSystemEntries(_folder.Path));
    }

    // A file that a program holds open cannot be removed: the caller is told, and the folder stays.
    [Fact]
    public void Remove_ADataFolderWithAFileInUse_Throws()
    {
        Directory.CreateDirectory(_folder.Combine("Bravo"));
        var lockedFile = _folder.Combine("Bravo", "lock");
        using (new FileStream(lockedFile, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            Assert.Throws<IOException>(() => _userData.Remove(_folder.Path));
        }

        Assert.True(File.Exists(lockedFile));
    }
}