using System;
using System.Collections.Generic;
using System.IO;
using Bravo.Setup.CommandLine;
using Bravo.Setup.ExternalTools;
using Bravo.Setup.Telemetry;
using Bravo.Setup.UserData;

namespace Bravo.Setup;

/// <summary>
/// Reads a command line, carries out its command and says how it went with an exit code.
/// </summary>
internal sealed class SetupApplication(
    IExternalToolRegistration externalTools,
    InstallationEventReporter reporter,
    IUserDataFolder userData,
    TextWriter error)
{
    private readonly IExternalToolRegistration _externalTools = externalTools;
    private readonly InstallationEventReporter _reporter = reporter;
    private readonly IUserDataFolder _userData = userData;
    private readonly TextWriter _error = error;

    public int Run(IReadOnlyList<string> arguments)
    {
        if (!SetupCommandLine.TryParse(arguments, out var command, out var parseError))
        {
            _error.WriteLine(parseError);
            return ExitCode.InvalidCommandLine;
        }

        try
        {
            Execute(command);
            return ExitCode.Success;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _error.WriteLine(exception.Message);
            return ExitCode.Failed;
        }
    }

    private void Execute(SetupCommand command)
    {
        switch (command)
        {
            case RegisterExternalToolCommand register:
                _externalTools.Register(register.ToolFile, register.Executable);
                break;
            case UnregisterExternalToolCommand unregister:
                _externalTools.Unregister(unregister.ToolFile, unregister.Executable);
                break;
            case ReportInstallationEventCommand report:
                _reporter.Report(report.Kind, report.Product, report.IsTelemetryEnabled);
                break;
            case RemoveUserDataCommand remove:
                _userData.Remove(remove.LocalApplicationData);
                break;
            default:
                throw new InvalidOperationException($"No code carries out the command {command.GetType().Name}.");
        }
    }
}
