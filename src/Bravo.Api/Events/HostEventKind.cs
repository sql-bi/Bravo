namespace Bravo.Api.Events;

public enum HostEventKind
{
    /// <summary>
    /// The application was started: the start that opened the window, or a later one handed over to it.
    /// </summary>
    Activation,

    /// <summary>
    /// The file of the settings of the user could not be read: the settings are the defaults, and the next change
    /// of the options replaces the file. Published once, at the start of the host.
    /// </summary>
    SettingsReadFailed,
}
