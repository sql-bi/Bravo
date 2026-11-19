using System;
using System.IO;
using System.Security;
using Microsoft.Win32;

namespace Bravo.Desktop.Shell;

/// <summary>
/// Reads the value of the registry with which a user of 1.0.x hides the version in the title of the window,
/// where 1.0.x reads it. No package and no option writes it: the user does.
/// </summary>
internal sealed class RegistryWindowTitleSettings : IWindowTitleSettings
{
    private const string KeyName = @"HKEY_CURRENT_USER\Software\SQLBI\Bravo";
    private const string ValueName = "applicationTitleVersionHidden";

    public bool IsVersionHidden
    {
        get
        {
            try
            {
                return IsVersionHiddenValue(Registry.GetValue(KeyName, ValueName, defaultValue: null));
            }
            catch (Exception exception) when (exception is SecurityException or IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// What a value of the registry says, as 1.0.x reads it: a DWORD of 1 hides the version, and anything else
    /// shows it.
    /// </summary>
    internal static bool IsVersionHiddenValue(object? value)
    {
        return value is int hidden && hidden == 1;
    }
}
