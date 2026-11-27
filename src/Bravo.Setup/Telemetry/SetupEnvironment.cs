using System;

namespace Bravo.Setup.Telemetry
{
    /// <summary>
    /// What the library reads of the machine and of the account that an action runs as.
    /// </summary>
    internal sealed class SetupEnvironment
    {
        /// <param name="machineName">The name of the machine.</param>
        /// <param name="userName">The name of the account, without its domain.</param>
        /// <param name="operatingSystem">The operating system and its version.</param>
        public SetupEnvironment(string machineName, string userName, string operatingSystem)
        {
            MachineName = machineName;
            UserName = userName;
            OperatingSystem = operatingSystem;
        }

        public string MachineName { get; }

        public string UserName { get; }

        public string OperatingSystem { get; }

        public static SetupEnvironment Read()
        {
            return new SetupEnvironment(Environment.MachineName, Environment.UserName, Environment.OSVersion.ToString());
        }
    }
}
