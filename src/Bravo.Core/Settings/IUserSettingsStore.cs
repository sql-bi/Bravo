namespace Bravo.Core.Settings;

/// <summary>
/// The settings of the user, as one value that is read and replaced as a whole.
/// </summary>
public interface IUserSettingsStore
{
    UserSettings Current { get; }

    void Replace(UserSettings settings);
}
