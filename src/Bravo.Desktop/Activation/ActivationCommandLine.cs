using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Bravo.Core.Activation;
using Bravo.Desktop.Development;

namespace Bravo.Desktop.Activation;

/// <summary>
/// The command line of the executable, as the external tool registration of Power BI Desktop writes it:
/// <c>--server</c> or <c>-s</c>, <c>--database</c> or <c>-d</c>, and <c>--ppid</c>. A development build also
/// knows <c>--dev-origin</c>, <c>--dev-connection-file</c>, <c>--dev-update-feed</c>, <c>--dev-data-folder</c>,
/// <c>--dev-policy-key</c> and <c>--dev-crash</c>.
/// </summary>
internal static class ActivationCommandLine
{
    /// <summary>
    /// Reads the arguments. An argument that is wrong becomes an error of the request and never stops the
    /// start; a start with no arguments is valid.
    /// </summary>
    /// <param name="args">The arguments, without the name of the program.</param>
    /// <param name="identity">
    /// The build that was started. Only a development build has the development options: to a release build
    /// they are arguments that it does not know, and so errors of the request.
    /// </param>
    public static StartArguments Parse(string[] args, BuildIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(identity);

        if (!identity.IsDevelopmentBuild)
        {
            return new StartArguments(ParseActivation(args, developmentErrors: []), DevelopmentOptions.None);
        }

        // The development options are read first, and what is left is the command line of every build.
        var origin = new Option<string>("--dev-origin");
        var connectionFile = new Option<string>("--dev-connection-file");
        var updateFeed = new Option<string>("--dev-update-feed");
        var dataFolder = new Option<string>("--dev-data-folder");
        var policyKey = new Option<string>("--dev-policy-key");
        var crash = new Option<DevelopmentCrash>("--dev-crash");
        var command = new Command("Bravo") { origin, connectionFile, updateFeed, dataFolder, policyKey, crash };
        command.TreatUnmatchedTokensAsErrors = false;
        var result = command.Parse(args, CreateConfiguration());

        var errors = result.Errors.Select(error => error.Message).ToList();
        var development = new DevelopmentOptions
        {
            AllowedOrigin = ReadOrigin(result, origin, errors),
            ConnectionFilePath = ReadPath(result, connectionFile, errors),
            UpdateFeedAddress = ReadAddress(result, updateFeed, errors),
            DataFolderPath = ReadPath(result, dataFolder, errors),
            PolicyKeyName = ReadKeyName(result, policyKey, errors),
            Crash = HasValue(result, crash) ? result.GetValue(crash) : null,
        };

        return new StartArguments(ParseActivation([.. result.UnmatchedTokens], errors), development);
    }

    private static ActivationRequest ParseActivation(string[] args, List<string> developmentErrors)
    {
        if (args.Length == 0 && developmentErrors.Count == 0)
        {
            return ActivationRequest.Empty;
        }

        if (args.Length == 0)
        {
            return new ActivationRequest { CommandLineErrors = [.. developmentErrors] };
        }

        var server = new Option<string>("--server", "-s") { Required = true };
        var database = new Option<string>("--database", "-d") { Required = true };
        var parentProcessId = new Option<int>("--ppid");

        // Not a root command, which adds the help and version options of a console program.
        var command = new Command("Bravo") { server, database, parentProcessId };
        var result = command.Parse(args, CreateConfiguration());

        return new ActivationRequest
        {
            ServerName = HasValue(result, server) ? result.GetValue(server) : null,
            DatabaseName = HasValue(result, database) ? result.GetValue(database) : null,
            ParentProcessId = HasValue(result, parentProcessId) ? result.GetValue(parentProcessId) : null,
            CommandLineErrors = [.. developmentErrors, .. result.Errors.Select(error => error.Message)],
        };
    }

    // No response file: an argument never makes the parser read the disk.
    private static ParserConfiguration CreateConfiguration()
    {
        return new ParserConfiguration { ResponseFileTokenReplacer = null };
    }

    // GetValue throws for an option with an error and returns the default of the type for a missing one.
    private static bool HasValue(ParseResult result, Option option)
    {
        var optionResult = result.GetResult(option);
        return optionResult is not null && !optionResult.Implicit && !optionResult.Errors.Any();
    }

    // An origin is a scheme, a host and a port, as a browser writes it in the Origin header: the API
    // compares the two as text.
    private static string? ReadOrigin(ParseResult result, Option<string> option, List<string> errors)
    {
        if (!HasValue(result, option))
        {
            return null;
        }

        var value = result.GetValue(option);
        var isOrigin = IsWebAddress(value, out var uri)
            && string.Equals(uri.GetLeftPart(UriPartial.Authority), value, StringComparison.OrdinalIgnoreCase);
        if (!isOrigin)
        {
            errors.Add($"The value of '{option.Name}' is not an origin, such as http://127.0.0.1:8098: '{value}'.");
            return null;
        }

        return uri!.GetLeftPart(UriPartial.Authority);
    }

    private static Uri? ReadAddress(ParseResult result, Option<string> option, List<string> errors)
    {
        if (!HasValue(result, option))
        {
            return null;
        }

        var value = result.GetValue(option);
        if (!IsWebAddress(value, out var address))
        {
            errors.Add($"The value of '{option.Name}' is not an address, such as http://127.0.0.1:8099/currentversion.json: '{value}'.");
            return null;
        }

        return address;
    }

    private static bool IsWebAddress(string? value, [NotNullWhen(true)] out Uri? address)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out address)
            && (address.Scheme == Uri.UriSchemeHttp || address.Scheme == Uri.UriSchemeHttps);
    }

    // A path under a root, as Software\Bravo.Tests\Policies: not empty, and without a root before it, whether
    // \, HKEY_CURRENT_USER\ or HKCU\.
    private static string? ReadKeyName(ParseResult result, Option<string> option, List<string> errors)
    {
        if (!HasValue(result, option))
        {
            return null;
        }

        var value = result.GetValue(option);
        var first = value?.Split('\\')[0];
        var isKeyName = !string.IsNullOrWhiteSpace(value)
            && first is { Length: > 0 }
            && !first.StartsWith("HKEY_", StringComparison.OrdinalIgnoreCase)
            && !first.Equals("HKCU", StringComparison.OrdinalIgnoreCase);
        if (!isKeyName)
        {
            errors.Add($"The value of '{option.Name}' is not a key under HKEY_CURRENT_USER, such as Software\\Bravo.Tests\\Policies: '{value}'.");
            return null;
        }

        return value;
    }

    private static string? ReadPath(ParseResult result, Option<string> option, List<string> errors)
    {
        if (!HasValue(result, option))
        {
            return null;
        }

        var value = result.GetValue(option);
        try
        {
            return Path.GetFullPath(value!);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            errors.Add($"The value of '{option.Name}' is not a path: '{value}'.");
            return null;
        }
    }
}
