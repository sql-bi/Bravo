using System;
using System.IO;
using System.Security;
using Microsoft.Win32;

namespace Bravo.Desktop.Shell;

/// <summary>
/// Reads the app mode where Windows keeps it for the user, from Windows 10 version 1809 on. The engine of
/// the webview reads the same value to tell a page its color scheme: the window and the page of a user who
/// chose no theme agree because of that.
/// </summary>
internal sealed class RegistrySystemAppMode : ISystemAppMode
{
    private const string KeyName = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string ValueName = "AppsUseLightTheme";

    public bool IsDark
    {
        get
        {
            try
            {
                return IsDarkValue(Registry.GetValue(KeyName, ValueName, defaultValue: null));
            }
            catch (Exception exception) when (exception is SecurityException or IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// What a value of the registry says. Zero is dark; a version of Windows that has no such value, and
    /// anything that is not a number, is light.
    /// </summary>
    internal static bool IsDarkValue(object? value)
    {
        return value is int usesLightTheme && usesLightTheme == 0;
    }
}
