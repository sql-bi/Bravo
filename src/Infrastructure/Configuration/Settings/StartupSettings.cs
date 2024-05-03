namespace Sqlbi.Bravo.Infrastructure.Configuration.Settings;

using Sqlbi.Bravo.Infrastructure.Extensions;
using Sqlbi.Bravo.Infrastructure.Helpers;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text.Json.Serialization;

internal class StartupSettings
{
    [JsonPropertyName("isEmpty")]
    public bool IsEmpty { get; set; }

    [JsonPropertyName("externalTool")]
    public bool IsExternalTool { get; set; }

    [JsonPropertyName("serverName")]
    public string? ArgumentServerName { get; set; }

    [JsonPropertyName("databaseName")]
    public string? ArgumentDatabaseName { get; set; }

    [JsonPropertyName("commandLineErrors")]
    public string[]? CommandLineErrors { get; set; }

    [JsonIgnore]
    public int? ParentProcessId { get; set; }

    [JsonIgnore]
    public string? ParentProcessImageName { get; set; }

    [JsonIgnore]
    public string? ParentProcessMainWindowTitle { get; set; }

    public static StartupSettings CreateFromCommandLineArguments()
    {
        var settings = new StartupSettings();
        settings.UpdateFromCommandLineArguments();
        return settings;
    }
}

internal static class StartupSettingsExtensions
{
    public static void UpdateFromCommandLineArguments(this StartupSettings settings)
    {
        // Skip the first command line arg as the first element in the array contains the file name of the executing program.
        // If the file name is not available, the first element is equal to String.Empty.
        // In.NET 5 and later versions, for single-file publishing, the first element is the name of the host executable.
        var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
        if (args.Length == 0)
        {
            // No args provided
            settings.IsEmpty = true;
            return;
        }

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

        var parseResult = command.Parse(args);

        var serverOptionResult = parseResult.FindResultFor(serverOption);
        if (serverOptionResult is not null && serverOptionResult.ErrorMessage is null)
            settings.ArgumentServerName = parseResult.GetValueForOption(serverOption);

        var databaseOptionResult = parseResult.FindResultFor(databaseOption);
        if (databaseOptionResult is not null && databaseOptionResult.ErrorMessage is null)
            settings.ArgumentDatabaseName = parseResult.GetValueForOption(databaseOption);

        settings.IsExternalTool = parseResult.HasOption(serverOption) || parseResult.HasOption(databaseOption);
        settings.CommandLineErrors = parseResult.Errors.Select((e) => e.Message).ToArray();

        //if (AppEnvironment.DeploymentMode == AppDeploymentMode.Packaged && parseResult.HasOption(parentProcessIdOption))
        //{
        //    var parentProcessId = parseResult.GetValueForOption(parentProcessIdOption);
        //    throw new NotImplementedException();
        //}

        if (settings.IsExternalTool && ProcessHelper.GetParentProcess() is { } parentProcess)
        {
            settings.ParentProcessId = parentProcess.Id;
            settings.ParentProcessImageName = parentProcess.ImageName;
            settings.ParentProcessMainWindowTitle = parentProcess.MainWindowTitle;
        }
    }
}
