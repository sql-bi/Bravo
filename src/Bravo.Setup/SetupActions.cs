using System;
using System.IO;
using Bravo.Setup.ExternalTools;
using Bravo.Setup.Telemetry;
using Bravo.Setup.UserData;
using WixToolset.Dtf.WindowsInstaller;

namespace Bravo.Setup
{
    /// <summary>
    /// What each action does with the data that the package sets for it: every key of the action, once, and no
    /// other. An action that cannot read its data, or meets the error of a file or of a folder, fails and says why.
    /// </summary>
    internal sealed class SetupActions
    {
        // The keys of the data, as the package names them.
        private const string ToolFile = "ToolFile";
        private const string Executable = "Executable";
        private const string Scope = "Scope";
        private const string ProductName = "ProductName";
        private const string ProductVersion = "ProductVersion";
        private const string ProductBuild = "ProductBuild";
        private const string TelemetryEnabled = "TelemetryEnabled";
        private const string LocalApplicationData = "LocalApplicationData";

        // The value of TelemetryEnabled when who installs chose to send: anything else is a no.
        private const string Enabled = "1";

        private readonly InstallationEventReporter _reporter;
        private readonly Action<string> _log;

        /// <param name="reporter">Reports the events of the installer.</param>
        /// <param name="log">Writes a line in the log of the installation.</param>
        public SetupActions(InstallationEventReporter reporter, Action<string> log)
        {
            if (reporter == null)
            {
                throw new ArgumentNullException(nameof(reporter));
            }

            if (log == null)
            {
                throw new ArgumentNullException(nameof(log));
            }

            _reporter = reporter;
            _log = log;
        }

        public ActionResult RegisterExternalTool(CustomActionData data)
        {
            return Run(data, () => ExternalToolRegistration.Register(data[ToolFile], data[Executable]), ToolFile, Executable);
        }

        public ActionResult UnregisterExternalTool(CustomActionData data)
        {
            return Run(data, () => ExternalToolRegistration.Unregister(data[ToolFile], data[Executable]), ToolFile, Executable);
        }

        public ActionResult ReportInstall(CustomActionData data)
        {
            return Report(InstallationEventKind.Install, data);
        }

        public ActionResult ReportUninstall(CustomActionData data)
        {
            return Report(InstallationEventKind.Uninstall, data);
        }

        public ActionResult RemoveUserData(CustomActionData data)
        {
            return Run(data, () => UserDataFolder.Remove(data[LocalApplicationData]), LocalApplicationData);
        }

        private ActionResult Report(InstallationEventKind kind, CustomActionData data)
        {
            return Run(data, () => Send(kind, data), Scope, ProductName, ProductVersion, ProductBuild, TelemetryEnabled);
        }

        private void Send(InstallationEventKind kind, CustomActionData data)
        {
            var product = new InstalledProduct(data[Scope], data[ProductName], data[ProductVersion], data[ProductBuild]);
            var isTelemetryEnabled = string.Equals(data[TelemetryEnabled], Enabled, StringComparison.Ordinal);
            _reporter.Report(kind, product, isTelemetryEnabled);
        }

        private ActionResult Run(CustomActionData data, Action action, params string[] keys)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var error = Check(data, keys);
            if (error == null)
            {
                try
                {
                    action();
                    return ActionResult.Success;
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
                {
                    error = exception.Message;
                }
            }

            _log(error);
            return ActionResult.Failure;
        }

        // DTF ends a value at a semicolon, and reads two as one: a value that has one, such as a folder of the
        // installation, is read as a value cut short and a key more, or as another value. The action fails rather
        // than act on a part of the value or on another path.
        private static string Check(CustomActionData data, string[] keys)
        {
            foreach (var key in keys)
            {
                if (!data.TryGetValue(key, out var value) || value == null)
                {
                    return $"The data of the action has no value for {key}.";
                }

                if (value.IndexOf(';') >= 0)
                {
                    return $"The value of {key} has a semicolon, which the data of an action cannot hold.";
                }

                if (IsPath(key) && !FullPath.IsFullyQualified(value))
                {
                    return $"The value of {key} is not a full path: '{value}'.";
                }
            }

            if (data.Count != keys.Length)
            {
                return $"The data of the action has {data.Count} keys instead of {keys.Length}: a key that the action does not read, or a value cut at a semicolon.";
            }

            return null;
        }

        private static bool IsPath(string key)
        {
            return key == ToolFile || key == Executable || key == LocalApplicationData;
        }
    }
}
