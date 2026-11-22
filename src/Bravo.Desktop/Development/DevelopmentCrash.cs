namespace Bravo.Desktop.Development;

/// <summary>
/// Where a development build throws an exception that nothing catches, when its command line asks for it with
/// <c>--dev-crash</c>: a test sees what a crash there leaves.
/// </summary>
internal enum DevelopmentCrash
{
    /// <summary>
    /// While the host starts.
    /// </summary>
    Start,

    /// <summary>
    /// On the thread of the window, once it is shown.
    /// </summary>
    Window,

    /// <summary>
    /// On a thread of its own.
    /// </summary>
    Thread,

    /// <summary>
    /// In a service of the background, once the window is shown: it stops the host.
    /// </summary>
    Service,

    /// <summary>
    /// In a task that nothing awaits: it is recorded, and the application goes on.
    /// </summary>
    Unobserved,
}
