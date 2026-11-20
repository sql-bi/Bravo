using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Security.Principal;
using Bravo.Platform.Windows.Policies;
using Microsoft.Win32;

namespace Bravo.Platform.Windows.Tests;

/// <summary>
/// A key of one test under <c>HKEY_CURRENT_USER\Software\Bravo.Tests</c>, removed with everything in it when the
/// test ends: no test reads or writes a key of the application or of its policies.
/// </summary>
internal sealed class TemporaryRegistryKey : IDisposable
{
    private const string ParentName = @"Software\Bravo.Tests";

    private readonly List<string> _deniedNames = [];

    public TemporaryRegistryKey()
    {
        Name = $@"{ParentName}\{Guid.NewGuid():N}";
        Registry.CurrentUser.CreateSubKey(Name).Dispose();
    }

    /// <summary>
    /// The path of the key under <c>HKEY_CURRENT_USER</c>.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// A key under this one, as the reader of the policies is given it. Nothing creates it.
    /// </summary>
    public RegistryPolicyKey PolicyKey(string subKeyName)
    {
        return new RegistryPolicyKey(Registry.CurrentUser, SubKeyPath(subKeyName));
    }

    public void SetValue(string subKeyName, string valueName, object value, RegistryValueKind kind)
    {
        using var key = Registry.CurrentUser.CreateSubKey(SubKeyPath(subKeyName));
        key.SetValue(valueName, value, kind);
    }

    /// <summary>
    /// Denies the user the reading of the values of a key under this one, as permissions that an administrator
    /// set wrong would: opening it to read fails.
    /// </summary>
    public void DenyReading(string subKeyName)
    {
        var path = SubKeyPath(subKeyName);
        using var key = Registry.CurrentUser.CreateSubKey(path);
        var security = key.GetAccessControl();
        security.AddAccessRule(CreateDenyRule());
        key.SetAccessControl(security);
        _deniedNames.Add(path);
    }

    public void Dispose()
    {
        // A key whose reading is denied cannot be deleted: it is opened to read its subkeys first.
        foreach (var path in _deniedNames)
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                path,
                RegistryKeyPermissionCheck.ReadWriteSubTree,
                RegistryRights.ReadPermissions | RegistryRights.ChangePermissions);
            if (key is null)
            {
                continue;
            }

            var security = key.GetAccessControl();
            security.RemoveAccessRuleSpecific(CreateDenyRule());
            key.SetAccessControl(security);
        }

        Registry.CurrentUser.DeleteSubKeyTree(Name, throwOnMissingSubKey: false);
        RemoveEmptyParent();
    }

    private static RegistryAccessRule CreateDenyRule()
    {
        var user = WindowsIdentity.GetCurrent().User ?? throw new InvalidOperationException("The process has no user.");
        return new RegistryAccessRule(user, RegistryRights.QueryValues, AccessControlType.Deny);
    }

    // Another test may add a key at the same time: the parent then stays for it to remove.
    private static void RemoveEmptyParent()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKey(ParentName, throwOnMissingSubKey: false);
        }
        catch (InvalidOperationException)
        {
        }
    }

    private string SubKeyPath(string subKeyName)
    {
        return $@"{Name}\{subKeyName}";
    }
}
