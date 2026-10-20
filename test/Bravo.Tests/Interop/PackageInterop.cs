using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Bravo.Tests.Interop;

[SupportedOSPlatform("windows8.1")]
internal static class PackageInterop
{
    /// <summary>
    /// Returns the installation folders of the packages of a family installed for the current user.
    /// </summary>
    /// <exception cref="Win32Exception">The packages cannot be read.</exception>
    public static IReadOnlyList<string> GetPackageFolders(string packageFamilyName)
    {
        uint count = 0;
        uint bufferLength = 0;
        var result = TestPInvoke.GetPackagesByPackageFamily(packageFamilyName, ref count, null, ref bufferLength, null);
        if (result == WIN32_ERROR.ERROR_SUCCESS)
            return []; // No package of the family is installed

        if (result != WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER)
            throw new Win32Exception((int)result);

        var packageFullNames = new PWSTR[count];
        var buffer = new char[bufferLength];
        result = TestPInvoke.GetPackagesByPackageFamily(packageFamilyName, ref count, packageFullNames, ref bufferLength, buffer);
        if (result != WIN32_ERROR.ERROR_SUCCESS)
            throw new Win32Exception((int)result);

        var folders = new List<string>();
        foreach (var packageFullName in new string(buffer).Split('\0', StringSplitOptions.RemoveEmptyEntries))
            folders.Add(GetPackagePath(packageFullName));

        return folders;
    }

    private static string GetPackagePath(string packageFullName)
    {
        uint pathLength = 0;
        var result = TestPInvoke.GetPackagePathByFullName(packageFullName, ref pathLength, null);
        if (result != WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER)
            throw new Win32Exception((int)result);

        var path = new char[pathLength];
        result = TestPInvoke.GetPackagePathByFullName(packageFullName, ref pathLength, path);
        if (result != WIN32_ERROR.ERROR_SUCCESS)
            throw new Win32Exception((int)result);

        return new string(path, 0, (int)pathLength - 1); // The length includes the null terminator
    }
}
