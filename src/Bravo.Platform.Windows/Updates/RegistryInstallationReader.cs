using System;
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
    private const string MachineKeyName = @"HKEY_LOCAL_MACHINE\Software\SQLBI\Bravo";
    private const string UserKeyName = @"HKEY_CURRENT_USER\Software\SQLBI\Bravo";
    private const string ValueName = "installFolder";

    private readonly string _applicationFolder = applicationFolder;

    public Installation Read()
    {
        return Classify(ReadFolder(MachineKeyName), ReadFolder(UserKeyName), _applicationFolder);
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

    private static string? ReadFolder(string keyName)
    {
        try
        {
            return Registry.GetValue(keyName, ValueName, defaultValue: null) as string;
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
