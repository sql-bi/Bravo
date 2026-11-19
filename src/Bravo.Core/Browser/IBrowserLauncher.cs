using System;

namespace Bravo.Core.Browser;

public interface IBrowserLauncher
{
    /// <summary>
    /// Opens an address in the default browser of the user.
    /// </summary>
    /// <param name="address">An address that <see cref="TrustedSites"/> contains.</param>
    void Open(Uri address);
}
