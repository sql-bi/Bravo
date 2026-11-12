using System;

namespace Bravo.Desktop.Shell;

/// <summary>
/// The main window, as the code that decides when it lives and when it comes forward sees it.
/// </summary>
internal interface IMainWindow
{
    /// <summary>
    /// Raised once, on the thread of the window, when the window is first shown.
    /// </summary>
    event EventHandler? Shown;

    /// <summary>
    /// Shows the window and returns when it has been closed.
    /// </summary>
    void RunMessageLoop();

    /// <summary>
    /// Asks the window to close. It can be called from any thread, before the window is shown and after it
    /// was closed.
    /// </summary>
    void RequestClose();

    /// <summary>
    /// Restores the window if it is minimized and brings it in front of the other windows. It can be called
    /// from any thread, and does nothing once the window was closed.
    /// </summary>
    void BringToForeground();
}
