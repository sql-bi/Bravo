using System;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Core.Errors;
using Bravo.Core.Settings;

namespace Bravo.Api.Tests;

/// <summary>
/// The settings of a test: they start from the defaults and last as long as the test. A test can make a save
/// fail as a file that cannot be written does.
/// </summary>
internal sealed class InMemoryUserSettingsStore : IUserSettingsStore
{
    private volatile UserSettings _current = UserSettings.Default;

    public event EventHandler? Changed;

    public UserSettings Current => _current;

    public bool SaveFails { get; set; }

    public Task ReplaceAsync(UserSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (SaveFails)
        {
            throw new BravoException(ErrorCode.SettingsSaveFailed, "The settings could not be saved.");
        }

        _current = settings;
        Changed?.Invoke(this, EventArgs.Empty);

        return Task.CompletedTask;
    }
}
