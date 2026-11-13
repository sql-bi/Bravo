using System;

namespace Bravo.Core.Settings;

/// <summary>
/// The settings of the user, as one value that is read and replaced as a whole.
/// </summary>
public interface IUserSettingsStore
{
    /// <summary>
    /// Raised after the settings were replaced, on the thread that replaced them. It says that they changed,
    /// not how: a handler reads <see cref="Current"/> when it acts, so that replacements close in time leave
    /// it with the last one, in whatever order it was told.
    /// </summary>
    event EventHandler? Changed;

    UserSettings Current { get; }

    void Replace(UserSettings settings);
}
