using System;

namespace Bravo.Platform.Windows.Policies;

/// <summary>
/// A key of the policies exists and cannot be read: the application does not run without the restrictions that
/// an administrator may have set there.
/// </summary>
public sealed class PolicyReadException : Exception
{
    /// <param name="keyName">The key that cannot be read, as an administrator reads it.</param>
    /// <param name="innerException">What the registry answered.</param>
    public PolicyReadException(string keyName, Exception innerException)
        : base($"The policies of the key {keyName} cannot be read.", innerException)
    {
        KeyName = keyName;
    }

    /// <summary>
    /// The key that cannot be read, such as <c>HKEY_LOCAL_MACHINE\Software\Policies\SQLBI\Bravo\OptionSettings</c>.
    /// </summary>
    public string KeyName { get; }
}
