namespace Bravo.Desktop.Shell;

/// <summary>
/// The color mode that Windows gives to applications: the "default app mode" of its color settings. It is
/// not the mode of Windows itself, which colors the taskbar and the Start menu and can differ.
/// </summary>
internal interface ISystemAppMode
{
    /// <summary>
    /// Whether applications are asked to be dark, read at every call: the user can change it at any time.
    /// </summary>
    bool IsDark { get; }
}
