namespace Bravo.Setup.CommandLine;

/// <summary>
/// Registers the application as an external tool of Power BI Desktop.
/// </summary>
/// <param name="ToolFile">The full path of the registration file to write.</param>
/// <param name="Executable">The full path of the executable that Power BI Desktop starts.</param>
internal sealed record RegisterExternalToolCommand(string ToolFile, string Executable) : SetupCommand;
