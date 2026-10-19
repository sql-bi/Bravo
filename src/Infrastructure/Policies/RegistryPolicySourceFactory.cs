using System;
using Microsoft.Win32;

namespace Sqlbi.Bravo.Infrastructure.Policies;

internal sealed class RegistryPolicySourceFactory : IPolicySourceFactory
{
    private const string OptionSettingsSubKeyName = @"SOFTWARE\Policies\SQLBI\Bravo\OptionSettings";

    public IPolicySource Open(PolicyScope scope)
    {
        var hive = scope switch
        {
            PolicyScope.Computer => Registry.LocalMachine,
            PolicyScope.User => Registry.CurrentUser,
            _ => throw new ArgumentOutOfRangeException(nameof(scope)),
        };

        return new RegistryPolicySource(hive.OpenSubKey(OptionSettingsSubKeyName));
    }
}
