using System;
using System.IO;
using System.Threading.Tasks;
using Bravo.Core.Errors;
using Bravo.Core.Settings;
using Bravo.Core.Updates;
using Bravo.Platform.Windows.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Xunit;

namespace Bravo.Platform.Windows.Tests.Settings;

public sealed class FileUserSettingsStoreTests : IDisposable
{
    private readonly TemporaryFolder _folder = new();
    private readonly IInstallationReader _installation = Substitute.For<IInstallationReader>();
    private readonly RecordingLogger<FileUserSettingsStore> _logger = new();
    private readonly string _path;

    public FileUserSettingsStoreTests()
    {
        _path = _folder.Combine("usersettings.json");
    }

    [Fact]
    public void Current_NoFile_IsTheDefaults()
    {
        using var store = CreateStore();

        Assert.Equal(UserSettings.Default, store.Current);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Current_NoFile_SendsTelemetryAsTheInstallationChose(bool chosen)
    {
        _installation.ReadTelemetryEnabled().Returns(chosen);
        using var store = CreateStore();

        Assert.Equal(chosen, store.Current.TelemetryEnabled);
    }

    [Fact]
    public void Current_FileOf1014_IsItsSettingsWhateverTheInstallationChose()
    {
        File.WriteAllText(_path, Release1014.ThemeOnlyFile);
        _installation.ReadTelemetryEnabled().Returns(false);
        using var store = CreateStore();

        Assert.Equal(UserSettings.Default with { Theme = Theme.Dark }, store.Current);
    }

    [Fact]
    public void Current_FileThatIsNotJson_IsTheDefaultsAndIsLogged()
    {
        File.WriteAllText(_path, "{ \"theme\": 2");
        using var store = CreateStore();

        Assert.Equal(UserSettings.Default, store.Current);
        Assert.Equal([(LogLevel.Warning, new EventId(1, "LogReadFailed"))], _logger.Entries);
    }

    // The host reads the file at its start, outside any request: the failure has a trace of its own.
    [Fact]
    public void Current_FileThatIsNotJson_IsLoggedUnderATrace()
    {
        File.WriteAllText(_path, "{ \"theme\": 2");
        using var store = CreateStore();

        _ = store.Current;

        Assert.NotNull(Assert.Single(_logger.TraceIds));
    }

    [Fact]
    public void Current_FileThatIsNotJson_LeavesTheFileAsItIs()
    {
        File.WriteAllText(_path, "{ \"theme\": 2");
        using var store = CreateStore();

        _ = store.Current;

        Assert.Equal("{ \"theme\": 2", File.ReadAllText(_path));
    }

    [Fact]
    public void ReadFailed_FileThatIsNotJson_IsTrue()
    {
        File.WriteAllText(_path, "not JSON");
        using var store = CreateStore();

        Assert.True(store.ReadFailed);
    }

    // Another program that holds the file without sharing it: the read fails as for a file that is not JSON.
    [Fact]
    public void Current_FileHeldByAnotherProgram_IsTheDefaultsAndTheReadFailed()
    {
        File.WriteAllText(_path, Release1014.ThemeOnlyFile);
        using var held = new FileStream(_path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        using var store = CreateStore();

        Assert.Equal(UserSettings.Default, store.Current);
        Assert.True(store.ReadFailed);
        Assert.Equal([(LogLevel.Warning, new EventId(1, "LogReadFailed"))], _logger.Entries);
    }

    [Fact]
    public void ReadFailed_NoFile_IsFalse()
    {
        using var store = CreateStore();

        Assert.False(store.ReadFailed);
    }

    [Fact]
    public async Task ReplaceAsync_Settings_AreWhatTheNextStartReads()
    {
        var settings = UserSettings.Default with { Theme = Theme.Light, DiagnosticLevel = DiagnosticLevel.Basic };
        using (var store = CreateStore())
        {
            await store.ReplaceAsync(settings, TestContext.Current.CancellationToken);
        }

        using var next = CreateStore();

        Assert.Equal(settings, next.Current);
    }

    [Fact]
    public async Task ReplaceAsync_FileOf1014_KeepsTheMembersThatAreNoSetting()
    {
        File.WriteAllText(_path, Release1014.EverythingChangedFile);
        using var store = CreateStore();

        await store.ReplaceAsync(UserSettings.Default, TestContext.Current.CancellationToken);

        Assert.Equal(Release1014.UpdateChannelType.Dev, Release1014.Read(File.ReadAllText(_path))!.UpdateChannel);
    }

    [Fact]
    public async Task ReplaceAsync_FileThatIsNotJson_ReplacesIt()
    {
        File.WriteAllText(_path, "not JSON");
        using var store = CreateStore();

        await store.ReplaceAsync(UserSettings.Default with { Theme = Theme.Dark }, TestContext.Current.CancellationToken);

        Assert.Equal(Release1014.ThemeType.Dark, Release1014.Read(File.ReadAllText(_path))!.Theme);
    }

    [Fact]
    public async Task ReplaceAsync_Settings_TellsThatTheyChangedOnceTheyAreTheCurrentOnes()
    {
        using var store = CreateStore();
        var settings = UserSettings.Default with { Theme = Theme.Dark };
        UserSettings? currentWhenTold = null;
        store.Changed += (_, _) => currentWhenTold = store.Current;

        await store.ReplaceAsync(settings, TestContext.Current.CancellationToken);

        Assert.Same(settings, currentWhenTold);
    }

    [Fact]
    public async Task ReplaceAsync_FileThatCannotBeWritten_ThrowsTheErrorOfASave()
    {
        using var store = CreateStoreThatCannotWrite();

        var exception = await Assert.ThrowsAsync<BravoException>(
            () => store.ReplaceAsync(UserSettings.Default with { Theme = Theme.Dark }, TestContext.Current.CancellationToken));

        Assert.Equal(ErrorCode.SettingsSaveFailed, exception.Code);
    }

    [Fact]
    public async Task ReplaceAsync_FileThatCannotBeWritten_KeepsTheSettingsAndTellsNothing()
    {
        using var store = CreateStoreThatCannotWrite();
        var told = 0;
        store.Changed += (_, _) => told++;

        await Assert.ThrowsAsync<BravoException>(
            () => store.ReplaceAsync(UserSettings.Default with { Theme = Theme.Dark }, TestContext.Current.CancellationToken));

        Assert.Equal(UserSettings.Default, store.Current);
        Assert.Equal(0, told);
    }

    public void Dispose()
    {
        _folder.Dispose();
    }

    private FileUserSettingsStore CreateStore(string? path = null)
    {
        var replacer = new FileReplacer(new FakeTimeProvider()) { Attempts = 1 };

        return new FileUserSettingsStore(path ?? _path, _installation, replacer, _logger);
    }

    // The folder of the file is a file: no attempt can write it.
    private FileUserSettingsStore CreateStoreThatCannotWrite()
    {
        var notAFolder = _folder.Combine("Bravo");
        File.WriteAllText(notAFolder, "");

        return CreateStore(Path.Combine(notAFolder, "usersettings.json"));
    }
}
