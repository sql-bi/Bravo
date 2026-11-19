using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Bravo.Platform.Windows.Settings;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Bravo.Platform.Windows.Tests.Settings;

public sealed class FileReplacerTests : IDisposable
{
    private static readonly TimeSpan s_retryDelay = TimeSpan.FromMilliseconds(100);

    private readonly TemporaryFolder _folder = new();
    private readonly FakeTimeProvider _time = new();

    [Fact]
    public async Task ReplaceAsync_FileAndFolderThatDoNotExist_WritesBoth()
    {
        var path = _folder.Combine("Bravo", "usersettings.json");

        await CreateReplacer().ReplaceAsync(path, Encoding.UTF8.GetBytes("new"), TestContext.Current.CancellationToken);

        Assert.Equal("new", await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReplaceAsync_FileThatExists_ReplacesItAndLeavesNoOtherFile()
    {
        var path = _folder.Combine("usersettings.json");
        await File.WriteAllTextAsync(path, "old content, longer than the new one", TestContext.Current.CancellationToken);

        await CreateReplacer().ReplaceAsync(path, Encoding.UTF8.GetBytes("new"), TestContext.Current.CancellationToken);

        Assert.Equal("new", await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
        Assert.Equal([path], Directory.GetFiles(_folder.Path));
    }

    [Fact]
    public async Task ReplaceAsync_FileHeldByAnotherProgramForAMoment_ReplacesItAtTheNextAttempt()
    {
        var path = _folder.Combine("usersettings.json");
        await File.WriteAllTextAsync(path, "old", TestContext.Current.CancellationToken);
        var held = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);

        var replacing = CreateReplacer(attempts: 2).ReplaceAsync(path, Encoding.UTF8.GetBytes("new"), TestContext.Current.CancellationToken);
        await held.DisposeAsync();
        _time.Advance(s_retryDelay);
        await replacing;

        Assert.Equal("new", await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReplaceAsync_FileHeldAtEveryAttempt_IsDeniedAndLeavesTheFileAsItWas()
    {
        var path = _folder.Combine("usersettings.json");
        await File.WriteAllTextAsync(path, "old", TestContext.Current.CancellationToken);

        using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => CreateReplacer(attempts: 1)
                .ReplaceAsync(path, Encoding.UTF8.GetBytes("new"), TestContext.Current.CancellationToken));
        }

        Assert.Equal("old", await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
        Assert.Equal([path], Directory.GetFiles(_folder.Path));
    }

    public void Dispose()
    {
        _folder.Dispose();
    }

    private FileReplacer CreateReplacer(int attempts = 5)
    {
        return new FileReplacer(_time) { Attempts = attempts, RetryDelay = s_retryDelay };
    }
}
