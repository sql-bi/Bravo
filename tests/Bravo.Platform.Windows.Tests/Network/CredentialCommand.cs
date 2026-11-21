using System;
using System.Diagnostics;
using Xunit;

namespace Bravo.Platform.Windows.Tests.Network;

/// <summary>
/// <c>cmdkey</c>, the program of Windows that keeps and deletes credentials: for a test, a writer that is not
/// the code that reads them. The tests of <c>Bravo.Desktop</c> compile the same file.
/// </summary>
internal static class CredentialCommand
{
    /// <summary>
    /// Keeps generic credentials under a name, in place of the ones before.
    /// </summary>
    public static void Write(string targetName, string userName, string password)
    {
        var (exitCode, output) = Run($"/generic:\"{targetName}\" /user:{userName} /pass:{password}");

        Assert.True(exitCode == 0, $"cmdkey ended with {exitCode}: {output}");
    }

    /// <summary>
    /// Deletes the credentials of a name. With none it does nothing.
    /// </summary>
    public static void Delete(string targetName)
    {
        _ = Run($"/delete:\"{targetName}\"");
    }

    // cmdkey reads its command line by itself, not as a list of arguments: a name with spaces is quoted
    // after the colon of its switch, and not as a whole argument.
    private static (int ExitCode, string Output) Run(string arguments)
    {
        var start = new ProcessStartInfo(Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\cmdkey.exe"), arguments)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
        };
        using var process = Process.Start(start);
        Assert.NotNull(process);
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return (process.ExitCode, output.Trim());
    }
}
