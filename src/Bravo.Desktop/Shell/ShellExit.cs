namespace Bravo.Desktop.Shell;

/// <summary>
/// The exit code of the process when the main window was closed. A window that the user closes leaves it as
/// it is; a window that ends the application for a failure of its own sets the code of that failure.
/// </summary>
internal sealed class ShellExit
{
    public int Code { get; set; } = ExitCode.Success;
}
