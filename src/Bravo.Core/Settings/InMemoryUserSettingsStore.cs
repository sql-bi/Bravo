using System;

namespace Bravo.Core.Settings;

/// <summary>
/// Settings that last as long as the process: they start from the defaults and are never written anywhere.
/// </summary>
public sealed class InMemoryUserSettingsStore : IUserSettingsStore
{
    private volatile UserSettings _current = UserSettings.Default;

    public UserSettings Current => _current;

    public void Replace(UserSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _current = settings;
    }
}
