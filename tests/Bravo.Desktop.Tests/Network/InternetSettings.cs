using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Networking.WinInet;
using Xunit;

namespace Bravo.Desktop.Tests.Network;

/// <summary>
/// The Internet settings of the user for the local network: the proxy server and the auto-configuration script
/// that Windows gives every application, .NET among them. A test changes them only on a runner of GitHub
/// Actions, which is thrown away after the run, and puts back what it found when it is disposed.
/// </summary>
internal sealed class InternetSettings : IDisposable
{
    private readonly Values _found;

    private InternetSettings(Values found)
    {
        _found = found;
    }

    /// <summary>
    /// Makes Windows name a proxy server for plain HTTP addresses alone: a secure request of any other
    /// program of the machine still goes to its server.
    /// </summary>
    /// <param name="port">The port of a proxy on the loopback address.</param>
    public static InternetSettings UseProxy(int port)
    {
        var flags = TestPInvoke.PROXY_TYPE_DIRECT | TestPInvoke.PROXY_TYPE_PROXY;

        return Change(new Values(flags, $"http=127.0.0.1:{port}", Bypass: null, Script: null));
    }

    /// <summary>
    /// Makes Windows name an auto-configuration script.
    /// </summary>
    public static InternetSettings UseScript(Uri script)
    {
        var flags = TestPInvoke.PROXY_TYPE_DIRECT | TestPInvoke.PROXY_TYPE_AUTO_PROXY_URL;

        return Change(new Values(flags, Server: null, Bypass: null, script.AbsoluteUri));
    }

    public void Dispose()
    {
        Write(_found);
    }

    private static InternetSettings Change(Values values)
    {
        var found = Read();
        var names = TestPInvoke.PROXY_TYPE_PROXY | TestPInvoke.PROXY_TYPE_AUTO_PROXY_URL;
        Assert.True((found.Flags & names) == 0, "The machine already has a proxy or a script in its Internet settings: the test replaces none.");

        Write(values);

        return new InternetSettings(found);
    }

    private static unsafe Values Read()
    {
        var options = stackalloc INTERNET_PER_CONN_OPTIONW[4];
        options[0].dwOption = INTERNET_PER_CONN.INTERNET_PER_CONN_FLAGS;
        options[1].dwOption = INTERNET_PER_CONN.INTERNET_PER_CONN_PROXY_SERVER;
        options[2].dwOption = INTERNET_PER_CONN.INTERNET_PER_CONN_PROXY_BYPASS;
        options[3].dwOption = INTERNET_PER_CONN.INTERNET_PER_CONN_AUTOCONFIG_URL;
        var list = CreateList(options);
        var size = list.dwSize;
        if (!TestPInvoke.InternetQueryOption(null, TestPInvoke.INTERNET_OPTION_PER_CONNECTION_OPTION, &list, ref size))
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        }

        return new Values(options[0].Value.dwValue, Take(options[1]), Take(options[2]), Take(options[3]));
    }

    private static unsafe void Write(Values values)
    {
        fixed (char* server = values.Server, bypass = values.Bypass, script = values.Script)
        {
            var options = stackalloc INTERNET_PER_CONN_OPTIONW[4];
            options[0].dwOption = INTERNET_PER_CONN.INTERNET_PER_CONN_FLAGS;
            options[0].Value.dwValue = values.Flags;
            options[1].dwOption = INTERNET_PER_CONN.INTERNET_PER_CONN_PROXY_SERVER;
            options[1].Value.pszValue = server;
            options[2].dwOption = INTERNET_PER_CONN.INTERNET_PER_CONN_PROXY_BYPASS;
            options[2].Value.pszValue = bypass;
            options[3].dwOption = INTERNET_PER_CONN.INTERNET_PER_CONN_AUTOCONFIG_URL;
            options[3].Value.pszValue = script;
            var list = CreateList(options);
            if (!TestPInvoke.InternetSetOption(null, TestPInvoke.INTERNET_OPTION_PER_CONNECTION_OPTION, &list, list.dwSize))
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            }
        }
    }

    // The connection without a name is the local network.
    private static unsafe INTERNET_PER_CONN_OPTION_LISTW CreateList(INTERNET_PER_CONN_OPTIONW* options)
    {
        return new INTERNET_PER_CONN_OPTION_LISTW
        {
            dwSize = (uint)sizeof(INTERNET_PER_CONN_OPTION_LISTW),
            dwOptionCount = 4,
            pOptions = options,
        };
    }

    // Windows allocates the text of an option that it returns: the caller frees it.
    private static unsafe string? Take(INTERNET_PER_CONN_OPTIONW option)
    {
        var text = option.Value.pszValue;
        if (text.Value is null)
        {
            return null;
        }

        var value = text.ToString();
        _ = TestPInvoke.GlobalFree((HGLOBAL)(nint)text.Value);

        return value;
    }

    private sealed record Values(uint Flags, string? Server, string? Bypass, string? Script);
}
