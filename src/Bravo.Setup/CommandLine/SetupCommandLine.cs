using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Bravo.Setup.Telemetry;

namespace Bravo.Setup.CommandLine;

/// <summary>
/// The command line of the program: the name of a command, then its options, each as <c>--name value</c>.
/// A command gets every option it has, exactly once, and no other.
/// </summary>
internal static class SetupCommandLine
{
    public const string RegisterExternalTool = "register-external-tool";
    public const string UnregisterExternalTool = "unregister-external-tool";
    public const string ReportInstall = "report-install";
    public const string ReportUninstall = "report-uninstall";
    public const string RemoveUserData = "remove-user-data";

    public const string ToolFileOption = "--tool-file";
    public const string ExecutableOption = "--executable";
    public const string ScopeOption = "--scope";
    public const string ProductNameOption = "--product-name";
    public const string ProductVersionOption = "--product-version";
    public const string ProductBuildOption = "--product-build";
    public const string TelemetryEnabledOption = "--telemetry-enabled";
    public const string FolderOption = "--folder";

    // The value of the telemetry option when who installs chose to send: anything else is a no.
    private const string Enabled = "1";

    public static bool TryParse(
        IReadOnlyList<string> arguments,
        [NotNullWhen(true)] out SetupCommand? command,
        [NotNullWhen(false)] out string? error)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        command = null;
        if (arguments.Count == 0)
        {
            error = "The command line names no command.";
            return false;
        }

        if (!SetupOptions.TryRead(arguments, out var options, out error))
        {
            return false;
        }

        command = arguments[0] switch
        {
            RegisterExternalTool => ReadRegisterExternalTool(options),
            UnregisterExternalTool => ReadUnregisterExternalTool(options),
            ReportInstall => ReadReport(InstallationEventKind.Install, options),
            ReportUninstall => ReadReport(InstallationEventKind.Uninstall, options),
            RemoveUserData => ReadRemoveUserData(options),
            _ => null,
        };

        if (command is null)
        {
            error = options.Error ?? $"'{arguments[0]}' is not a command.";
            return false;
        }

        error = options.Error ?? options.DescribeUnread();
        if (error is not null)
        {
            command = null;
            return false;
        }

        return true;
    }

    private static RegisterExternalToolCommand? ReadRegisterExternalTool(SetupOptions options)
    {
        var toolFile = options.ReadFullPath(ToolFileOption);
        var executable = options.ReadFullPath(ExecutableOption);
        if (toolFile is null || executable is null)
        {
            return null;
        }

        return new RegisterExternalToolCommand(toolFile, executable);
    }

    private static UnregisterExternalToolCommand? ReadUnregisterExternalTool(SetupOptions options)
    {
        var toolFile = options.ReadFullPath(ToolFileOption);
        return toolFile is null ? null : new UnregisterExternalToolCommand(toolFile);
    }

    private static ReportInstallationEventCommand? ReadReport(InstallationEventKind kind, SetupOptions options)
    {
        var scope = options.Read(ScopeOption);
        var name = options.Read(ProductNameOption);
        var version = options.Read(ProductVersionOption);
        var build = options.Read(ProductBuildOption);
        var telemetry = options.Read(TelemetryEnabledOption);
        if (scope is null || name is null || version is null || build is null || telemetry is null)
        {
            return null;
        }

        var product = new InstalledProduct(scope, name, version, build);
        var isTelemetryEnabled = string.Equals(telemetry, Enabled, StringComparison.Ordinal);
        return new ReportInstallationEventCommand(kind, product, isTelemetryEnabled);
    }

    private static RemoveUserDataCommand? ReadRemoveUserData(SetupOptions options)
    {
        var folder = options.ReadFullPath(FolderOption);
        if (folder is null)
        {
            return null;
        }

        // A drive or a share, which has no folder above it, is never the data folder of the application.
        if (Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(folder)) is null)
        {
            options.Reject($"The option {FolderOption} names the root of a drive: '{folder}'.");
            return null;
        }

        return new RemoveUserDataCommand(folder);
    }
}
