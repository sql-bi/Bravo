using System;
using System.Collections.Generic;
using System.IO;
using Bravo.Tests.Interop;

namespace Bravo.Tests.Infrastructure.Services.PowerBI;

/// <summary>
/// Finds the Power BI Desktop installations on this machine: the MSI one and the Microsoft Store package.
/// </summary>
internal static class PowerBIDesktopInstallation
{
    private const string PackageFamilyName = "Microsoft.MicrosoftPowerBIDesktop_8wekyb3d8bbwe";

    /// <summary>
    /// Returns the <c>bin</c> folder of every installation.
    /// </summary>
    public static IEnumerable<string> GetBinFolders()
    {
        var programFilesFolder = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var installerFolder = Path.Combine(programFilesFolder, "Microsoft Power BI Desktop", "bin");
        if (Directory.Exists(installerFolder))
            yield return installerFolder;

        // The Store package of Power BI Desktop requires Windows 10.
        if (!OperatingSystem.IsWindowsVersionAtLeast(10))
            yield break;

        // The package folder is under WindowsApps, which cannot be listed, so its path comes from the package.
        foreach (var packageFolder in PackageInterop.GetPackageFolders(PackageFamilyName))
        {
            var binFolder = Path.Combine(packageFolder, "bin");
            if (Directory.Exists(binFolder))
                yield return binFolder;
        }
    }
}
