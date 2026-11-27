using System;
using System.Runtime.InteropServices;

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
            return new SetupEnvironment(Environment.MachineName, Environment.UserName, ReadOperatingSystem());
        }

        // Environment.OSVersion of .NET Framework is the version that the manifest of the process supports, and
        // the rundll32 of an action declares none: Windows 8. RuntimeInformation describes the system that runs,
        // as "Microsoft Windows 10.0.26100 ", and its version is written as .NET writes Environment.OSVersion.
        private static string ReadOperatingSystem()
        {
            var words = RuntimeInformation.OSDescription.Split(' ');
            if (words.Length > 2 && Version.TryParse(words[2], out var version) && version.Build >= 0)
            {
                return new OperatingSystem(PlatformID.Win32NT, new Version(version.Major, version.Minor, version.Build, 0)).ToString();
            }

            return Environment.OSVersion.ToString();
        }
    }
}
