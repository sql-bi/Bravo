using System;
using System.Collections.Generic;
using Bravo.Desktop.Development;
using Bravo.Platform.Windows.Policies;
using Microsoft.Win32;

namespace Bravo.Desktop.Policies;

internal static class PolicyKeys
{
    /// <summary>
    /// The keys where a build reads its policies: a release build the ones that administrators set, a development
    /// build only the one that its command line names, so that the policies of the machine never reach a test.
    /// </summary>
    public static IReadOnlyList<RegistryPolicyKey> For(BuildIdentity identity, DevelopmentOptions development)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(development);

        if (!identity.IsDevelopmentBuild)
        {
            return RegistryPolicyReader.ApplicationKeys;
        }

        if (development.PolicyKeyName is { } keyName)
        {
            return [new RegistryPolicyKey(Registry.CurrentUser, keyName)];
        }

        return [];
    }
}
