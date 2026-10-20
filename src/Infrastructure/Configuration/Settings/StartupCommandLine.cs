using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;

namespace Sqlbi.Bravo.Infrastructure.Configuration.Settings;

/// <summary>
/// The arguments Bravo was started with, as a Power BI Desktop External Tool passes them.
/// </summary>
internal sealed class StartupCommandLine
{
    private StartupCommandLine()
    {
    }

    public bool IsEmpty { get; private init; }

    /// <summary>
    /// Whether <c>--server</c> or <c>--database</c> was passed, even with a missing value.
    /// </summary>
    public bool HasConnectionArguments { get; private init; }

    public string? ServerName { get; private init; }

    public string? DatabaseName { get; private init; }

    public int? ParentProcessId { get; private init; }

    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <param name="args">The arguments without the program name.</param>
    public static StartupCommandLine Parse(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (args.Count == 0)
            return new StartupCommandLine { IsEmpty = true };

        var serverOption = new Option<string>("--server");
        serverOption.AddAlias("-s");
        serverOption.IsRequired = true;
        serverOption.Description = "Server name";
        serverOption.Arity = ArgumentArity.ExactlyOne;

        var databaseOption = new Option<string>("--database");
        databaseOption.AddAlias("-d");
        databaseOption.IsRequired = true;
        databaseOption.Description = "Database name";
        databaseOption.Arity = ArgumentArity.ExactlyOne;

        var parentProcessIdOption = new Option<int>("--ppid");
        parentProcessIdOption.IsRequired = false;
        parentProcessIdOption.Description = "Parent process ID";
        parentProcessIdOption.Arity = ArgumentArity.ExactlyOne;

        var command = new RootCommand
        {
            serverOption,
            databaseOption,
            parentProcessIdOption
        };

        var parseResult = command.Parse(args.ToArray());

        return new StartupCommandLine
        {
            HasConnectionArguments = parseResult.HasOption(serverOption) || parseResult.HasOption(databaseOption),
            ServerName = HasValidValue(parseResult, serverOption) ? parseResult.GetValueForOption(serverOption) : null,
            DatabaseName = HasValidValue(parseResult, databaseOption) ? parseResult.GetValueForOption(databaseOption) : null,
            ParentProcessId = HasValidValue(parseResult, parentProcessIdOption) ? parseResult.GetValueForOption(parentProcessIdOption) : null,
            Errors = parseResult.Errors.Select((e) => e.Message).ToArray(),
        };
    }

    // GetValueForOption throws when the value is missing or cannot be converted, so it is read only without an error.
    private static bool HasValidValue(ParseResult parseResult, Option option)
    {
        var optionResult = parseResult.FindResultFor(option);
        if (optionResult is null)
            return false;

        return optionResult.ErrorMessage is null;
    }
}
