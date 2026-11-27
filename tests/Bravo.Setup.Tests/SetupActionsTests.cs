using System;
using System.Collections.Generic;
using System.IO;
using Bravo.Setup.Telemetry;
using NSubstitute;
using WixToolset.Dtf.WindowsInstaller;
using Xunit;

namespace Bravo.Setup.Tests
{
    /// <summary>
    /// The actions as the entry points compose them, with the sink of the events replaced and the log kept: real
    /// files, in the folder of the test. The data is written as the package writes it.
    /// </summary>
    public sealed class SetupActionsTests : IDisposable
    {
        private const string Executable = @"C:\Program Files\SQLBI\Bravo\Bravo.exe";

        private readonly TemporaryFolder _folder = new TemporaryFolder();
        private readonly IInstallationEventSink _sink = Substitute.For<IInstallationEventSink>();
        private readonly List<string> _log = new List<string>();
        private readonly SetupActions _actions;

        public SetupActionsTests()
        {
            var environment = new SetupEnvironment("LAPTOP-12C9A7VU", "SYSTEM", "Microsoft Windows NT 10.0.26100.0");
            _actions = new SetupActions(new InstallationEventReporter(_sink, environment), _log.Add);
        }

        public void Dispose()
        {
            _folder.Dispose();
        }

        [Fact]
        public void RegisterThenUnregister_WritesTheFileThenRemovesIt()
        {
            var toolFile = _folder.Combine("External Tools", "bravo.pbitool.json");
            var data = $"ToolFile={toolFile};Executable={Executable}";

            var registered = _actions.RegisterExternalTool(new CustomActionData(data));
            var wasWritten = File.Exists(toolFile);
            var unregistered = _actions.UnregisterExternalTool(new CustomActionData(data));

            Assert.Equal(ActionResult.Success, registered);
            Assert.Equal(ActionResult.Success, unregistered);
            Assert.True(wasWritten);
            Assert.False(File.Exists(toolFile));
            Assert.Empty(_log);
        }

        [Theory]
        [InlineData(nameof(SetupActions.ReportInstall), "Install")]
        [InlineData(nameof(SetupActions.ReportUninstall), "Uninstall")]
        public void Report_TelemetryEnabled_SendsTheEventOfTheProduct(string action, string name)
        {
            var data = new CustomActionData("Scope=perUser;ProductName=Bravo;ProductVersion=1.1.0;ProductBuild=1.1.0.12345;TelemetryEnabled=1");

            var result = Report(action, data);

            Assert.Equal(ActionResult.Success, result);
            _sink.Received(1).Send(Arg.Is<InstallationEvent>(sent =>
                sent.Name == name &&
                sent.Product.InstallScope == "perUser" &&
                sent.Product.Name == "Bravo" &&
                sent.Product.Version == "1.1.0" &&
                sent.Product.Build == "1.1.0.12345"));
        }

        // The property of the choice is empty when it was turned off in the dialog.
        [Theory]
        [InlineData("0")]
        [InlineData("")]
        [InlineData("yes")]
        public void Report_TelemetryNotEnabled_SendsNothing(string telemetryEnabled)
        {
            var data = new CustomActionData($"Scope=perUser;ProductName=Bravo;ProductVersion=1.1.0;ProductBuild=1.1.0.0;TelemetryEnabled={telemetryEnabled}");

            var result = _actions.ReportInstall(data);

            Assert.Equal(ActionResult.Success, result);
            _sink.DidNotReceiveWithAnyArgs().Send(null);
        }

        [Fact]
        public void RemoveUserData_RemovesTheDataFolderOfTheApplication()
        {
            var dataFolder = _folder.Combine("Bravo");
            Directory.CreateDirectory(dataFolder);
            File.WriteAllText(Path.Combine(dataFolder, "usersettings.json"), "{}");

            var result = _actions.RemoveUserData(new CustomActionData($@"LocalApplicationData={_folder.Path}\"));

            Assert.Equal(ActionResult.Success, result);
            Assert.False(Directory.Exists(dataFolder));
        }

        [Fact]
        public void AnAction_DataWithoutAKey_FailsAndSaysWhichKey()
        {
            var toolFile = _folder.Combine("bravo.pbitool.json");

            var result = _actions.RegisterExternalTool(new CustomActionData($"ToolFile={toolFile}"));

            Assert.Equal(ActionResult.Failure, result);
            var line = Assert.Single(_log);
            Assert.Contains("Executable", line, StringComparison.Ordinal);
            Assert.Empty(Directory.GetFileSystemEntries(_folder.Path));
        }

        // DTF reads the folder as C:\Programs, followed by a key that has no value.
        [Fact]
        public void AnAction_AValueWithASemicolon_FailsRatherThanActOnAPartOfIt()
        {
            var toolFile = _folder.Combine("bravo.pbitool.json");

            var result = _actions.RegisterExternalTool(new CustomActionData($@"ToolFile={toolFile};Executable=C:\Programs;Tools\Bravo.exe"));

            Assert.Equal(ActionResult.Failure, result);
            var line = Assert.Single(_log);
            Assert.Contains("semicolon", line, StringComparison.Ordinal);
            Assert.Empty(Directory.GetFileSystemEntries(_folder.Path));
        }

        [Fact]
        public void AnAction_AKeyOfAnotherAction_Fails()
        {
            var data = new CustomActionData($"LocalApplicationData={_folder.Path};Scope=perUser");

            var result = _actions.RemoveUserData(data);

            Assert.Equal(ActionResult.Failure, result);
            Assert.Single(_log);
        }

        // A property that the package did not set leaves the action with no data at all.
        [Fact]
        public void AnAction_NoData_Fails()
        {
            var result = _actions.RemoveUserData(new CustomActionData());

            Assert.Equal(ActionResult.Failure, result);
            Assert.Single(_log);
        }

        // The package ignores the result: an action that cannot be carried out never fails an installation, and the
        // log says why.
        [Fact]
        public void AnAction_AFileThatCannotBeWritten_FailsAndWritesTheErrorInTheLog()
        {
            var toolFile = _folder.Combine("bravo.pbitool.json");
            Directory.CreateDirectory(toolFile);

            var result = _actions.RegisterExternalTool(new CustomActionData($"ToolFile={toolFile};Executable={Executable}"));

            Assert.Equal(ActionResult.Failure, result);
            Assert.NotEmpty(Assert.Single(_log));
        }

        // The message of .NET Framework names the file without its folder, in the language of the system.
        [Fact]
        public void AnAction_ADataFolderWithAFileInUse_FailsAndWritesTheErrorInTheLog()
        {
            Directory.CreateDirectory(_folder.Combine("Bravo"));
            var lockedFile = _folder.Combine("Bravo", "lock");
            ActionResult result;
            using (new FileStream(lockedFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                result = _actions.RemoveUserData(new CustomActionData($"LocalApplicationData={_folder.Path}"));
            }

            Assert.Equal(ActionResult.Failure, result);
            var line = Assert.Single(_log);
            Assert.Contains("'lock'", line, StringComparison.Ordinal);
        }

        private ActionResult Report(string action, CustomActionData data)
        {
            return action == nameof(SetupActions.ReportInstall) ? _actions.ReportInstall(data) : _actions.ReportUninstall(data);
        }
    }
}
