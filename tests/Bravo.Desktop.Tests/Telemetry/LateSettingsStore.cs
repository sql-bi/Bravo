using System;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Core.Settings;

namespace Bravo.Desktop.Tests.Telemetry;

/// <summary>
/// Settings whose change still reaches a handler that was removed, as a change already running on another
/// thread does.
/// </summary>
internal sealed class LateSettingsStore(IUserSettingsStore inner) : IUserSettingsStore
{
    private readonly IUserSettingsStore _inner = inner;

    public event EventHandler? Changed
    {
        add => _inner.Changed += value;
        remove { }
    }

    public UserSettings Current => _inner.Current;

    public Task ReplaceAsync(UserSettings settings, CancellationToken cancellationToken)
    {
        return _inner.ReplaceAsync(settings, cancellationToken);
    }
}
