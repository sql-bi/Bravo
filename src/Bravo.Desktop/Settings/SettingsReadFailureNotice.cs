using System.Threading;
using System.Threading.Tasks;
using Bravo.Api.Events;
using Bravo.Platform.Windows.Settings;
using Microsoft.Extensions.Hosting;

namespace Bravo.Desktop.Settings;

/// <summary>
/// Reads the settings when the host starts and, when their file could not be read, tells the page: the user
/// would otherwise find the defaults with no reason.
/// </summary>
internal sealed class SettingsReadFailureNotice(FileUserSettingsStore store, IHostEventPublisher events) : IHostedService
{
    private readonly FileUserSettingsStore _store = store;
    private readonly IHostEventPublisher _events = events;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_store.ReadFailed)
        {
            _events.PublishSettingsReadFailed();
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
