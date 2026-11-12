namespace Bravo.Desktop;

/// <summary>
/// The exit codes of the process. An exception that escapes ends the process with the code of the runtime.
/// </summary>
internal static class ExitCode
{
    /// <summary>
    /// The application ran and was closed, or this process handed its start over to the running instance.
    /// </summary>
    public const int Success = 0;

    /// <summary>
    /// Another process owns the instance and this start could not be handed over to it.
    /// </summary>
    public const int ActivationNotDelivered = 1;
}
