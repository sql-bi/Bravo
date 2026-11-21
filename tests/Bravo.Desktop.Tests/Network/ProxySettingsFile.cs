using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Bravo.Desktop.Tests.Network;

/// <summary>
/// The file of the settings with a proxy server of the user, as the application keeps it: the kind of the
/// proxy is a number there.
/// </summary>
internal static class ProxySettingsFile
{
    private const int CustomProxy = 2;

    /// <summary>
    /// Writes the file in a data folder, before the application that reads it starts.
    /// </summary>
    /// <param name="dataFolder">The data folder of the test.</param>
    /// <param name="proxy">The address of the proxy server, which no address goes around.</param>
    /// <param name="useDefaultCredentials">Whether the proxy server is answered as the Windows user.</param>
    public static Task WriteAsync(TemporaryFolder dataFolder, Uri proxy, bool useDefaultCredentials)
    {
        var settings = new
        {
            proxy = new { type = CustomProxy, useDefaultCredentials, address = proxy.AbsoluteUri, bypassOnLocal = false },
        };

        return File.WriteAllTextAsync(
            dataFolder.Combine("usersettings.json"),
            JsonSerializer.Serialize(settings),
            TestContext.Current.CancellationToken);
    }
}
