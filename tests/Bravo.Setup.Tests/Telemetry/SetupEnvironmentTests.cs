using Bravo.Setup.Telemetry;
using Xunit;

namespace Bravo.Setup.Tests.Telemetry
{
    public sealed class SetupEnvironmentTests
    {
        // Every version of Windows that the packages install on is 10.0, whatever the manifest of the process.
        [Fact]
        public void Read_TheOperatingSystem_IsTheVersionOfWindowsAsDotNetWritesIt()
        {
            var environment = SetupEnvironment.Read();

            Assert.Matches(@"^Microsoft Windows NT 10\.0\.\d+\.0$", environment.OperatingSystem);
        }
    }
}
