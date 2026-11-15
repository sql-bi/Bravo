using System;

namespace Bravo.Setup.Telemetry;

/// <summary>
/// What the program reads of the machine and of the account that it runs as.
/// </summary>
/// <param name="MachineName">The name of the machine.</param>
/// <param name="UserName">The name of the account, without its domain.</param>
/// <param name="OperatingSystem">The operating system and its version.</param>
internal sealed record SetupEnvironment(string MachineName, string UserName, string OperatingSystem)
{
    public static SetupEnvironment Read()
    {
        return new SetupEnvironment(Environment.MachineName, Environment.UserName, Environment.OSVersion.ToString());
    }
}
