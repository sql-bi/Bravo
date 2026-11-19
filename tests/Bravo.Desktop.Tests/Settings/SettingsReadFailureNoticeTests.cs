using System;
using System.IO;
using System.Threading.Tasks;
using Bravo.Api.Events;
using Bravo.Core.Updates;
using Bravo.Desktop.Settings;
using Bravo.Platform.Windows.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Bravo.Desktop.Tests.Settings;

public sealed class SettingsReadFailureNoticeTests : IDisposable
{
    private readonly TemporaryFolder _dataFolder = new();
    private readonly IHostEventPublisher _events = Substitute.For<IHostEventPublisher>();

    [Fact]
    public async Task StartAsync_FileThatIsNotJson_TellsThePage()
    {
        File.WriteAllText(_dataFolder.Combine("usersettings.json"), "not JSON");
        using var store = CreateStore();

        await new SettingsReadFailureNotice(store, _events).StartAsync(TestContext.Current.CancellationToken);

        _events.Received(1).PublishSettingsReadFailed();
    }

    [Fact]
    public async Task StartAsync_NoFile_TellsNothing()
    {
        using var store = CreateStore();

        await new SettingsReadFailureNotice(store, _events).StartAsync(TestContext.Current.CancellationToken);

        _events.DidNotReceive().PublishSettingsReadFailed();
    }

    public void Dispose()
    {
        _dataFolder.Dispose();
    }

    private FileUserSettingsStore CreateStore()
    {
        return new FileUserSettingsStore(
            _dataFolder.Combine("usersettings.json"),
            Substitute.For<IInstallationReader>(),
            new FileReplacer(TimeProvider.System),
            NullLogger<FileUserSettingsStore>.Instance);
    }
}
