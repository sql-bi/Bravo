using Microsoft.Win32;

namespace Bravo.Platform.Windows.Policies;

/// <summary>
/// A key of the registry where policies of the application can be set.
/// </summary>
/// <param name="Root">The root of the key, such as <see cref="Registry.LocalMachine"/>.</param>
/// <param name="Name">The path of the key under its root.</param>
public readonly record struct RegistryPolicyKey(RegistryKey Root, string Name)
{
    /// <summary>
    /// The key as an administrator reads it, such as <c>HKEY_LOCAL_MACHINE\Software\Policies\SQLBI\Bravo\OptionSettings</c>.
    /// </summary>
    public string FullName => $@"{Root.Name}\{Name}";
}
