namespace Bravo.Setup;

/// <summary>
/// The exit codes of the process. The installer packages ignore them, so that no command fails an
/// installation, and Windows Installer writes them in its log.
/// </summary>
internal static class ExitCode
{
    /// <summary>
    /// The command was carried out, also when it found nothing to do.
    /// </summary>
    public const int Success = 0;

    /// <summary>
    /// The command could not be carried out: a file or a folder could not be written or removed.
    /// </summary>
    public const int Failed = 1;

    /// <summary>
    /// The command line names no command, an unknown one, or lacks one of its options.
    /// </summary>
    public const int InvalidCommandLine = 2;
}
