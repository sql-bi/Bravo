using System;
using System.Diagnostics;
using Bravo.Core.Browser;

namespace Bravo.Platform.Windows.Browser;

/// <summary>
/// Opens an address with the program that Windows associates with web addresses, the default browser.
/// </summary>
public sealed class DefaultBrowserLauncher : IBrowserLauncher
{
    public void Open(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);

        // The shell starts whatever a text names, a program as well as a page: only a web address of a
        // trusted site reaches it, written with every character escaped.
        if (!TrustedSites.Contains(address))
        {
            throw new ArgumentException("The address is not the one of a page on a trusted site.", nameof(address));
        }

        var startInfo = new ProcessStartInfo(address.AbsoluteUri) { UseShellExecute = true };
        Process.Start(startInfo)?.Dispose();
    }
}
