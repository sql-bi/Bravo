using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Bravo.Setup.Telemetry;

namespace Bravo.Setup.CommandLine;

/// <summary>
/// The command line of the program: the name of a command, then its options, each as <c>--name value</c>.
/// A command gets every option it has, exactly once, and no other.
/// </summary>
internal static class SetupCommandLine
{
    private const string RegisterExternalTool = "register-external-tool";
    private const string UnregisterExternalTool = "unregister-external-tool";
    private const string ReportInstall = "report-install";
    private const string ReportUninstall = "report-uninstall";
    private const string RemoveUserData = "remove-user-data";

    private const string ToolFileOption = "--tool-file";
    private const string ExecutableOption = "--executable";
    private const string ScopeOption = "--scope";
    private const string ProductNameOption = "--product-name";
    private const string ProductVersionOption = "--product-version";
    private const string ProductBuildOption = "--product-build";
    private const string TelemetryEnabledOption = "--telemetry-enabled";
    private const string LocalApplicationDataOption = "--local-application-data";

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

        // A reader that returns no command has said why, unless no reader knows the command.
        if (command is null)
        {
            error = options.Error ?? $"'{arguments[0]}' is not a command.";
            return false;
        }

        error = options.DescribeUnread();
        if (error is null)
        {
            return true;
        }

        command = null;
        return false;
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
        var localApplicationData = options.ReadFullPath(LocalApplicationDataOption);
        return localApplicationData is null ? null : new RemoveUserDataCommand(localApplicationData);
    }
}
