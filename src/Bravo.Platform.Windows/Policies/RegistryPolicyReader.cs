using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using Bravo.Core.Policies;
using Microsoft.Win32;

namespace Bravo.Platform.Windows.Policies;

/// <summary>
/// Reads the policies of the application from keys of the registry, with the names and the values that the
/// templates write and that 1.0.x reads. A value of a key wins over the same value of the keys after it. A
/// switch is a DWORD of 1 or 0 and the folder a string: a value of another kind or number is not configured in
/// that key.
/// </summary>
public static class RegistryPolicyReader
{
    /// <summary>
    /// The key of the policies of the application, under each of the two roots.
    /// </summary>
    public const string KeyName = @"Software\Policies\SQLBI\Bravo\OptionSettings";

    private const string TelemetryEnabledName = "TelemetryEnabled";
    private const string UpdateCheckEnabledName = "UpdateCheckEnabled";
    private const string UseSystemBrowserForAuthenticationName = "UseSystemBrowserForAuthentication";
    private const string BuiltInTemplatesEnabledName = "BuiltInTemplatesEnabled";
    private const string CustomTemplatesEnabledName = "CustomTemplatesEnabled";
    private const string CustomTemplatesOrganizationRepositoryPathName = "CustomTemplatesOrganizationRepositoryPath";

    /// <summary>
    /// Where an administrator sets the policies of the application: for the computer, which wins, then for the
    /// user.
    /// </summary>
    public static IReadOnlyList<RegistryPolicyKey> ApplicationKeys { get; } =
        [new(Registry.LocalMachine, KeyName), new(Registry.CurrentUser, KeyName)];

    /// <param name="keys">The keys, the one that wins first. A key that does not exist sets nothing.</param>
    /// <exception cref="PolicyReadException">A key exists and cannot be read.</exception>
    public static ApplicationPolicies Read(IReadOnlyList<RegistryPolicyKey> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);

        var policies = ApplicationPolicies.None;
        foreach (var key in keys)
        {
            policies = Combine(policies, ReadKey(key));
        }

        return policies;
    }

    private static ApplicationPolicies ReadKey(RegistryPolicyKey location)
    {
        try
        {
            using var key = location.Root.OpenSubKey(location.Name);
            if (key is null)
            {
                return ApplicationPolicies.None;
            }

            return new ApplicationPolicies
            {
                TelemetryEnabled = ReadSwitch(key, TelemetryEnabledName),
                UpdateCheckEnabled = ReadSwitch(key, UpdateCheckEnabledName),
                UseSystemBrowserForAuthentication = ReadSwitch(key, UseSystemBrowserForAuthenticationName),
                BuiltInTemplatesEnabled = ReadSwitch(key, BuiltInTemplatesEnabledName),
                CustomTemplatesEnabled = ReadSwitch(key, CustomTemplatesEnabledName),
                CustomTemplatesOrganizationRepositoryPath = ReadText(key, CustomTemplatesOrganizationRepositoryPathName),
            };
        }
        catch (Exception exception) when (exception is SecurityException or IOException or UnauthorizedAccessException)
        {
            throw new PolicyReadException(location.FullName, exception);
        }
    }

    // A DWORD comes as an int; another kind comes as another type, a QWORD of 1 as a long.
    private static bool? ReadSwitch(RegistryKey key, string name)
    {
        return key.GetValue(name) switch
        {
            1 => true,
            0 => false,
            _ => null,
        };
    }

    // An expandable string is another kind, as for 1.0.x, and a string is kept as it was written.
    private static string? ReadText(RegistryKey key, string name)
    {
        var value = key.GetValue(name, defaultValue: null, RegistryValueOptions.DoNotExpandEnvironmentNames);
        if (value is not string text)
        {
            return null;
        }

        return key.GetValueKind(name) == RegistryValueKind.String ? text : null;
    }

    private static ApplicationPolicies Combine(ApplicationPolicies first, ApplicationPolicies then)
    {
        return new ApplicationPolicies
        {
            TelemetryEnabled = first.TelemetryEnabled ?? then.TelemetryEnabled,
            UpdateCheckEnabled = first.UpdateCheckEnabled ?? then.UpdateCheckEnabled,
            UseSystemBrowserForAuthentication = first.UseSystemBrowserForAuthentication ?? then.UseSystemBrowserForAuthentication,
            BuiltInTemplatesEnabled = first.BuiltInTemplatesEnabled ?? then.BuiltInTemplatesEnabled,
            CustomTemplatesEnabled = first.CustomTemplatesEnabled ?? then.CustomTemplatesEnabled,
            CustomTemplatesOrganizationRepositoryPath =
                first.CustomTemplatesOrganizationRepositoryPath ?? then.CustomTemplatesOrganizationRepositoryPath,
        };
    }
}
