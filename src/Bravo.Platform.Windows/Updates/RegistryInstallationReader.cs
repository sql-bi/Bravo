using System;
using System.Globalization;
using System.IO;
using System.Security;
using Bravo.Core.Updates;
using Microsoft.Win32;

namespace Bravo.Platform.Windows.Updates;

/// <summary>
/// Reads how the copy was installed from the value that each package writes, <c>installFolder</c>: under
/// <c>HKEY_LOCAL_MACHINE</c> for the package per machine, under <c>HKEY_CURRENT_USER</c> for the one per user.
/// A value counts only when it names the folder of the copy that runs: another copy may be installed too.
/// </summary>
/// <param name="applicationFolder">The folder of the copy that runs.</param>
public sealed class RegistryInstallationReader(string applicationFolder) : IInstallationReader
{
    private const string KeyName = @"Software\SQLBI\Bravo";
    private const string FolderValueName = "installFolder";
    private const string TelemetryValueName = "applicationTelemetryEnabled";

    private readonly string _applicationFolder = applicationFolder;

    public Installation Read()
    {
        var machineFolder = ReadText(Registry.LocalMachine, FolderValueName);
        var userFolder = ReadText(Registry.CurrentUser, FolderValueName);

        return Classify(machineFolder, userFolder, _applicationFolder);
    }

    /// <summary>
    /// Reads the choice under the key of the installation of the copy that runs, as every earlier version
    /// does.
    /// </summary>
    public bool? ReadTelemetryEnabled()
    {
        var root = Read() switch
        {
            Installation.PerMachine => Registry.LocalMachine,
            Installation.PerUser => Registry.CurrentUser,
            _ => null,
        };
        if (root is null)
        {
            return null;
        }

        var text = ReadText(root, TelemetryValueName);

        return text is null ? null : IsTelemetryEnabled(text);
    }

    /// <summary>
    /// What the two values say of a folder. The package per machine is asked first, as by every earlier
    /// version.
    /// </summary>
    internal static Installation Classify(string? machineFolder, string? userFolder, string applicationFolder)
    {
        if (IsSameFolder(machineFolder, applicationFolder))
        {
            return Installation.PerMachine;
        }

        if (IsSameFolder(userFolder, applicationFolder))
        {
            return Installation.PerUser;
        }

        return Installation.None;
    }

    /// <summary>
    /// What a recorded choice says: a number other than zero is yes, and any other text is no. The packages
    /// write "1" or nothing.
    /// </summary>
    internal static bool IsTelemetryEnabled(string text)
    {
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) && number != 0;
    }

    // A value that is not a string is no value, as for every earlier version: not even an expandable string.
    private static string? ReadText(RegistryKey root, string valueName)
    {
        try
        {
            using var key = root.OpenSubKey(KeyName);
            var value = key?.GetValue(valueName, defaultValue: null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            if (value is null || key!.GetValueKind(valueName) != RegistryValueKind.String)
            {
                return null;
            }

            return (string)value;
        }
        catch (Exception exception) when (exception is SecurityException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    // Windows Installer writes the folder with a separator at its end, and a folder name has no case.
    private static bool IsSameFolder(string? recorded, string applicationFolder)
    {
        if (string.IsNullOrWhiteSpace(recorded))
        {
            return false;
        }

        try
        {
            return string.Equals(Normalize(recorded), Normalize(applicationFolder), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    private static string Normalize(string folder)
    {
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder.Trim()));
    }
}
