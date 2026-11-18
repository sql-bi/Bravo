namespace Bravo.Setup.CommandLine;

/// <summary>
/// Removes the registration of the application as an external tool of Power BI Desktop.
/// </summary>
/// <param name="ToolFile">The full path of the registration file to remove.</param>
/// <param name="Executable">
/// The full path of the executable of this installation: a file that registers another one is left.
/// </param>
internal sealed record UnregisterExternalToolCommand(string ToolFile, string Executable) : SetupCommand;
