using System;
using System.CommandLine;
using System.Linq;
using Bravo.Core.Activation;

namespace Bravo.Desktop.Activation;

/// <summary>
/// The command line of the executable, as the external tool registration of Power BI Desktop writes it:
/// <c>--server</c> or <c>-s</c>, <c>--database</c> or <c>-d</c>, and <c>--ppid</c>.
/// </summary>
internal static class ActivationCommandLine
{
    /// <summary>
    /// Reads the arguments into a request. An argument that is wrong becomes an error of the request and never
    /// stops the start; a start with no arguments is valid.
    /// </summary>
    /// <param name="args">The arguments, without the name of the program.</param>
    public static ActivationRequest Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (args.Length == 0)
        {
            return ActivationRequest.Empty;
        }

        var server = new Option<string>("--server", "-s") { Required = true };
        var database = new Option<string>("--database", "-d") { Required = true };
        var parentProcessId = new Option<int>("--ppid");

        // Not a root command, which adds the help and version options of a console program.
        var command = new Command("Bravo") { server, database, parentProcessId };

        // No response file: an argument never makes the parser read the disk.
        var configuration = new ParserConfiguration { ResponseFileTokenReplacer = null };
        var result = command.Parse(args, configuration);

        return new ActivationRequest
        {
            ServerName = HasValue(result, server) ? result.GetValue(server) : null,
            DatabaseName = HasValue(result, database) ? result.GetValue(database) : null,
            ParentProcessId = HasValue(result, parentProcessId) ? result.GetValue(parentProcessId) : null,
            CommandLineErrors = [.. result.Errors.Select(error => error.Message)],
        };
    }

    // GetValue throws for an option with an error and returns the default of the type for a missing one.
    private static bool HasValue(ParseResult result, Option option)
    {
        var optionResult = result.GetResult(option);
        return optionResult is not null && !optionResult.Implicit && !optionResult.Errors.Any();
    }
}
