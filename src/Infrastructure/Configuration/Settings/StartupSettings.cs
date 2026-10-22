using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Serialization;
using Sqlbi.Bravo.Infrastructure.Extensions;
using Sqlbi.Bravo.Infrastructure.Helpers;
using Sqlbi.Bravo.Infrastructure.Services.PowerBI;

namespace Sqlbi.Bravo.Infrastructure.Configuration.Settings;

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
    public bool IsPBIDesktopExternalTool => IsExternalTool && PowerBIDesktop.ProcessName.Equals(ParentProcessName, StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public int? ParentProcessId { get; set; }

    [JsonIgnore]
    public string? ParentProcessName { get; set; }

    //[JsonIgnore]
    //public IntPtr ParentProcessMainWindowHandle { get; set; }

    [JsonIgnore]
    public string? ParentProcessMainWindowTitle { get; set; }

    /// <param name="args">The arguments without the program name.</param>
    public static StartupSettings CreateFromCommandLineArguments(IReadOnlyList<string> args)
    {
        var settings = new StartupSettings();
        {
            settings.FromCommandLineArguments(args);
        }

        return settings;
    }
}

internal static class StartupSettingsExtensions
{
    public static void FromCommandLineArguments(this StartupSettings settings, IReadOnlyList<string> args)
    {
        var commandLine = StartupCommandLine.Parse(args);

        using var currentProcess = Process.GetCurrentProcess();

        settings.FromCommandLine(commandLine, currentProcess);
    }

    // Does not run the AppEnvironment static constructor: a secondary instance builds its activation payload from these
    // settings before it exits.
    public static void FromCommandLine(this StartupSettings settings, StartupCommandLine commandLine, Process currentProcess)
    {
        if (commandLine.IsEmpty)
        {
            settings.IsEmpty = true;
            return;
        }

        settings.ArgumentServerName = commandLine.ServerName;
        settings.ArgumentDatabaseName = commandLine.DatabaseName;
        settings.IsExternalTool = commandLine.HasConnectionArguments;
        settings.CommandLineErrors = commandLine.Errors.ToArray();

        using var parentProcess = GetParentProcess(commandLine, currentProcess);
        if (parentProcess is not null)
        {
            settings.ParentProcessId = parentProcess.Id;
            settings.ParentProcessName = parentProcess.ProcessName;
            //settings.ParentProcessMainWindowHandle = parentProcess.MainWindowHandle;
            var windowTitle = parentProcess.GetMainWindowTitle();
            settings.ParentProcessMainWindowTitle = settings.IsPBIDesktopExternalTool ? PowerBIDesktop.GetReportName(windowTitle) : windowTitle;
        }
    }

    private static Process? GetParentProcess(StartupCommandLine commandLine, Process currentProcess)
    {
        if (commandLine.ParentProcessId is not int parentProcessId || !DesktopBridgeHelper.IsRunningAsMsixPackage())
            return currentProcess.GetParentProcess();

        if (!Process.TryGetById(parentProcessId, currentProcess.SessionId, out var parentProcess))
            return null;

        return parentProcess;
    }
}
