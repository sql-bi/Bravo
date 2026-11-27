using System;
using Bravo.Setup.Telemetry;
using WixToolset.Dtf.WindowsInstaller;

namespace Bravo.Setup
{
    /// <summary>
    /// The entry points of the actions that the installer packages run, each in a process of its own. An action
    /// reads the data that the package sets for it; the packages ignore how it ends, and Windows Installer writes
    /// in its log why it failed.
    /// </summary>
    public static class CustomActions
    {
        [CustomAction]
        public static ActionResult RegisterExternalTool(Session session)
        {
            return Compose(session).RegisterExternalTool(session.CustomActionData);
        }

        [CustomAction]
        public static ActionResult UnregisterExternalTool(Session session)
        {
            return Compose(session).UnregisterExternalTool(session.CustomActionData);
        }

        [CustomAction]
        public static ActionResult ReportInstall(Session session)
        {
            return Compose(session).ReportInstall(session.CustomActionData);
        }

        [CustomAction]
        public static ActionResult ReportUninstall(Session session)
        {
            return Compose(session).ReportUninstall(session.CustomActionData);
        }

        [CustomAction]
        public static ActionResult RemoveUserData(Session session)
        {
            return Compose(session).RemoveUserData(session.CustomActionData);
        }

        private static SetupActions Compose(Session session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

#if BRAVO_RELEASE_BUILD
            // Only the build of the packages sends, to the resource of the application.
            var destination = TelemetryDestination.Parse(TelemetryDestination.ReleaseConnectionString);
            IInstallationEventSink sink = new HttpInstallationEventSink(destination, HttpInstallationEventSink.DefaultTimeout);
#else
            IInstallationEventSink sink = new NullInstallationEventSink();
#endif
            var reporter = new InstallationEventReporter(sink, SetupEnvironment.Read());
            return new SetupActions(reporter, message => Log(session, message));
        }

        /// <summary>
        /// A line of the log. The message is the field of the record, which the log gets as it is: as the format of
        /// the record, a name in brackets in it would be read as a property.
        /// </summary>
        internal static Record CreateLogRecord(string message)
        {
            var record = new Record(1) { FormatString = "[1]" };
            record.SetString(1, message);
            return record;
        }

        private static void Log(Session session, string message)
        {
            using (var record = CreateLogRecord(message))
            {
                session.Message(InstallMessage.Info, record);
            }
        }
    }
}
