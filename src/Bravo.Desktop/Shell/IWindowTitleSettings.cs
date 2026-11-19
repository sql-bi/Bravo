namespace Bravo.Desktop.Shell;

/// <summary>
/// What the user chose for the title of the main window.
/// </summary>
internal interface IWindowTitleSettings
{
    /// <summary>
    /// Whether the title leaves out the version of the application.
    /// </summary>
    bool IsVersionHidden { get; }
}
