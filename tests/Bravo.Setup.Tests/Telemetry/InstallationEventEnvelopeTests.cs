using System;
using System.Text;
using Bravo.Setup.Telemetry;
using Xunit;

namespace Bravo.Setup.Tests.Telemetry
{
    public sealed class InstallationEventEnvelopeTests
    {
        // The item of an event with the names of the SDK of 1.0.x, and with nothing that names the computer.
        [Fact]
        public void Write_Event_IsTheItemOfApplicationInsightsWithTheContextOfVersion10()
        {
            var installationEvent = new InstallationEvent(
                "Install",
                "e4f87d099028e128ea2b413f4fa6fc741426bef314de588b0920e89f22015bd0",
                "Microsoft Windows NT 10.0.26100.0",
                new InstalledProduct("perUser", "Bravo", "1.1.0-beta.1", "1.1.0.12345"));
            var time = new DateTimeOffset(2026, 11, 23, 17, 45, 30, 125, TimeSpan.FromHours(1));
            var session = new Guid("9a5e9a2c-3f0b-4f7e-8d2a-6c1b2e3f4a5b");

            var item = InstallationEventEnvelope.Write(installationEvent, "00000000-0000-0000-0000-000000000001", time, session);

            const string Expected =
                "{\"name\":\"Microsoft.ApplicationInsights.00000000000000000000000000000001.Event\"," +
                "\"time\":\"2026-11-23T16:45:30.1250000Z\"," +
                "\"iKey\":\"00000000-0000-0000-0000-000000000001\"," +
                "\"tags\":{\"ai.user.id\":\"e4f87d099028e128ea2b413f4fa6fc741426bef314de588b0920e89f22015bd0\"," +
                "\"ai.session.id\":\"9a5e9a2c-3f0b-4f7e-8d2a-6c1b2e3f4a5b\"," +
                "\"ai.device.osVersion\":\"Microsoft Windows NT 10.0.26100.0\"," +
                "\"ai.application.ver\":\"1.1.0-beta.1\"}," +
                "\"data\":{\"baseType\":\"EventData\",\"baseData\":{\"ver\":2,\"name\":\"Install\"," +
                "\"properties\":{\"InstallScope\":\"perUser\",\"ProductName\":\"Bravo\",\"Version\":\"1.1.0-beta.1\",\"Build\":\"1.1.0.12345\"}}}}";
            Assert.Equal(Expected, Encoding.UTF8.GetString(item));
        }
    }
}
