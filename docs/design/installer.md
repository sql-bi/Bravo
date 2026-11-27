# Installer

Four Windows Installer packages, built with WiX, install the application
([ADR 0020](../decisions/0020-windows-installer-packages-built-with-wix-which-run-a-program-of-the-host.md)).

**Packages.**

| File | Installs | .NET runtime |
| --- | --- | --- |
| `Bravo.<version>.x64.msi` | Per machine | Carried by the package |
| `Bravo.<version>.x64-frameworkdependent.msi` | Per machine | The one that is installed |
| `Bravo.<version>.x64-userinstaller.msi` | Per user | Carried by the package |
| `Bravo.<version>.x64-frameworkdependent-userinstaller.msi` | Per user | The one that is installed |

- The names are the ones of 1.0.x: an installed 1.0.x asks for its update by a file name that it builds
  itself, for its own context and runtime.
- The four have the UpgradeCode of 1.0.x, and a new product code at every build. The version of the product
  is the version of the file of `Bravo.exe`. Windows Installer compares its first three numbers, which stay
  above the ones of 1.0.x: 1.0.14 is 1.0.9805.
- The product is `Bravo for Power BI`, and `Bravo for Power BI (User)` in the packages per user.
- A package installs on Windows 10 version 1607, on Windows Server 2016 and on what came after them, build
  14393: the oldest Windows that both .NET 10 and the WebView2 runtime support. It is its one condition: no
  package looks for the .NET runtime or for the WebView2 runtime, and none installs them.
- A package that uses the installed runtime needs the .NET Desktop Runtime and the ASP.NET Core Runtime,
  version 10: without them the application does not start.
- The packages are for x64 only.

**What a package installs.**

| | Per machine | Per user |
| --- | --- | --- |
| Folder | `%ProgramFiles%\SQLBI\Bravo` | `%LOCALAPPDATA%\Programs\SQLBI\Bravo` |
| Registry key | `HKLM\Software\SQLBI\Bravo` | `HKCU\Software\SQLBI\Bravo` |
| Shortcuts | On the desktop and in the Start menu of every user | Of the user |
| External tool of Power BI Desktop | Registered | Not registered: the folder needs administrator rights |
| Rights that the installation asks for | Administrator | None |

- The files are the application published for `win-x64` with the identity of a release build,
  `wwwroot`: the static files of the page with the release bundle, and `policies`: the templates of the
  policies, for administrators ([policies](policies.md)).
- Three things of a publish are left out, because nothing reads them when the application runs: the
  documentation files of the assemblies, the texts of System.CommandLine in other languages, so that an
  error of the command line is in English, and the second copy of the loader of WebView2, under `runtimes`:
  the process loads the one beside the executable.
- The symbols of the four assemblies of the host are installed, as 1.0.x installs them: the trace of an
  error names its lines.
- The registry values are the ones of 1.0.x: `installFolder`, by which the application tells how it was
  installed, and the four choices of who installs, each `1` or empty.

**Choices.** The public properties are the ones of 1.0.x, and a command line that 1.0.x understands has the
same effect.

| Property | What it chooses |
| --- | --- |
| `INSTALLFOLDER` | The folder of the application |
| `DESKTOPSHORTCUTENABLED` | A shortcut on the desktop |
| `PROGRAMMENUSHORTCUTENABLED` | A shortcut in the Start menu |
| `INSTALLERTELEMETRYENABLED` | The telemetry of the installer: the two events |
| `APPLICATIONTELEMETRYENABLED` | The telemetry of the application, as its first setting |
| `TELEMETRYUSERID` | Nothing: it is accepted and not read, as in 1.0.x |

- For the four choices, `1` is yes and any other value is no. Without a value, a first installation says
  yes; an upgrade and a removal read what the installed package wrote in the registry.
- With its dialogs, a package shows the licence, asks for the folder and the two shortcuts, then for the two
  choices on telemetry, and at the end offers to start the application.

**Upgrade.** A package replaces an installed package of its own context whose version is lower or the same:
the installed one is removed with all its files, then the new one is installed. So 1.1.0 installs over
1.0.14 with no manual uninstall, in the folder and with the choices of 1.0.14, and the data folder of the
user is not touched.

- A package never replaces an installation of the other context: it installs beside it, and Windows lists
  two products.
- A package of a lower version is refused, with a message.
- Windows Installer compares three numbers, and every build of 1.1.0 has the same three: the release of a
  version replaces a preview of it, and a preview installed over the release replaces it as well.
- Where Windows no longer has its copy of the installed package, the upgrade asks for that file
  ([issue 705](https://github.com/sql-bi/Bravo/issues/705)).

**What a package runs.** A package has no code of its own. What its tables cannot do, it asks of
`Bravo.Setup.CA.dll`, a custom action library of the host on .NET Framework 4.7.2, as 1.0.x, that the package
carries in itself and does not install
([ADR 0029](../decisions/0029-the-packages-run-a-custom-action-library-on-net-framework-which-writes-its-errors-in-the-log.md)):
it needs no .NET runtime, so that it runs where the application could not start, and it writes its errors in
the log of the installation.

| Action | Data | What it does |
| --- | --- | --- |
| `RegisterExternalTool` | `ToolFile`, `Executable` | Writes the file that registers the application as an external tool of Power BI Desktop, with its folder when it is missing. A file that is there is replaced |
| `UnregisterExternalTool` | `ToolFile`, `Executable` | Removes that file when it registers the executable. A file that registers another executable, such as one written by hand for another installation, or that is not a registration, stays. The folder stays: other tools register in it |
| `ReportInstall`, `ReportUninstall` | `Scope`, `ProductName`, `ProductVersion`, `ProductBuild`, `TelemetryEnabled` | Sends the event `Install` or `Uninstall`, when the value of `TelemetryEnabled` is `1` |
| `RemoveUserData` | `LocalApplicationData` | Removes the data folder of the application, `Bravo`, with everything in it, from the folder of local application data that it is given, and nothing else of that folder |

| When | Per machine | Per user |
| --- | --- | --- |
| An installation, also over an earlier version, after its files are copied | `RegisterExternalTool` | |
| A first installation that fails and is undone | `UnregisterExternalTool` | |
| An installation, also over an earlier version, that has succeeded | `ReportInstall` | `ReportInstall` |
| A removal that is not part of an upgrade and has succeeded | `UnregisterExternalTool`, `RemoveUserData`, `ReportUninstall` | `RemoveUserData`, `ReportUninstall` |

- An action reads its data, `Key=value` pairs separated by semicolons, from `CustomActionData`. It gets each of
  its keys once, and no other; a path is a full path. A value cannot hold a semicolon: DTF cuts a value at
  one and reads two as one. Data that breaks one of these rules fails the action, which says why in the log.
- The library finds no folder by itself and reads nothing from the registry: the package passes every path,
  and a test passes folders of its own. One name is in the library and not in the package, the one of the
  data folder: no data makes the library remove another folder.
- The registration is the one of 1.0.x: the name `Bravo`, the arguments
  `--server="%server%" --database="%database%"`, the same icon, and the full path of the executable. Its
  description is `Bravo for Power BI`. The file is `bravo.pbitool.json`, in the folder that Power BI
  Desktop reads, under the 32-bit Common Files: the package writes there with the rights of the
  installation.
- Every action loads the copy of the library that the package holds in itself, whoever it runs as: nothing
  that an installation or a removal runs is a file of a folder that somebody could have written in. Only the
  registration and its removal run with the rights of the installation. DTF runs an action in a process of
  its own, which ends with the action: nothing of the library stays in the process of Windows Installer.
- What cannot be taken back is done when nothing can fail any more: the removal of the registration, the
  removal of the data of the user and the two events are
  [commit actions](https://learn.microsoft.com/windows/win32/msi/commit-custom-actions), which Windows
  Installer runs after the installation or the removal has succeeded. A removal that fails and is undone
  leaves the data of the user and the registration as they were.
- Where rollback is disabled, by the property `DISABLEROLLBACK` or by the policy, Windows Installer undoes
  nothing and runs no commit action: the package then runs the same actions as the last steps of the
  installation or of the removal.
- An event carries what 1.0.x sends with it: its name, the scope, the name, the version and the build of the
  product, the operating system, and an identifier of who installs, the SHA-256 of the names of the machine
  and of the account that the action runs as. It names nobody, and is computed as 1.0.x computes it, so
  that the counts of the two versions can be compared, by the SHA-256 of Windows: the managed one, which
  .NET Framework gives an action,
  [throws](https://learn.microsoft.com/dotnet/framework/migration-guide/retargeting/4.8.x#managed-cryptography-classes-do-not-throw-a-cryptographyexception-in-fips-mode)
  on a machine that allows only FIPS algorithms. The version of
  Windows is the one that runs, as `RuntimeInformation.OSDescription` gives it, written as the application
  writes it: the process of an action declares no version of Windows in its manifest, and
  `Environment.OSVersion` of .NET Framework gives it
  [Windows 8](https://learn.microsoft.com/windows/win32/sysinfo/operating-system-version), which 1.0.x sent.
  A package reports, and removes the data of the user, as the account that installs or removes. Where a
  deployment tool installs in the system context, that account is the system: the identifier of an event is
  then one for the machine.
- The library of the packages sends an event to the resource of the application
  ([telemetry](telemetry.md)) once, through the proxy of the system with the Windows credentials of that
  account, and waits for it 5 seconds at most: an event that was not sent by then, or whose send failed in
  any way, is lost, and nothing is kept for later; the send never fails the action. The send adds TLS 1.2 to
  the protocols that .NET Framework gives an action, those of version 4.0, unless the machine lets the
  system choose. Any other build of the library sends nothing.
- As in 1.0.x, the choice of who installs alone decides whether the events are sent: the policy
  `TelemetryEnabled` is for the application, and the text that administrators read speaks of it alone
  ([policies](policies.md)). A removal follows the choice of the installation, which the registry keeps,
  where 1.0.x sent the event `Uninstall` whatever was chosen.
- On a server of Remote Desktop, Windows Installer runs an action of a package per machine as the system
  unless the action is
  [TerminalServerAware](https://learn.microsoft.com/windows/win32/msi/using-windows-installer-with-a-terminal-server):
  the two events are, so that they count who installs there too.
- An action succeeds when it was carried out, also when it found nothing to do. It fails when a file or a
  folder could not be written or removed, or when its data is not valid: it then writes why in the log of
  the installation, as one line, and Windows Installer writes `returned actual error code 1603` after it. An
  exception that nothing expected fails the action as well: DTF writes it in the log, with its stack trace.
  A package ignores the result: no action fails an installation.
- The message of .NET Framework can name a file without its folder, as in `'file-in-use.txt'`: the data of
  the action, which the log holds before it, gives the folder.

**Removal.** A removal takes away the files of the package, the shortcuts, the registry values, the entry
among the installed applications and, per machine, the registration as an external tool; the folder of the
external tools stays. A removal that is not part of an upgrade also removes the data folder of who removes,
`%LOCALAPPDATA%\Bravo`, with the settings, as the removal of 1.0.x does, per machine and per user. The data
folders of the other users of the machine stay. A removal that a deployment tool runs in the system context
is the removal of the system, which has no such folder: it leaves the data folder of every user.

**A running application.** A silent installation closes, through Restart Manager, the applications that
hold the files it replaces, and the application ends when asked to
([a request to end](startup.md)): an upgrade over a running application needs no
restart of Windows. The application is not started again.

**How the packages are built.** One WiX project makes the four, from one source for the two contexts. It
first lays the application out in a folder, as a package installs it: the application published for
`win-x64` and the files of the page. The library is built apart, and DTF packs it with what it needs into one
file. The build validates each package against the rules of Windows Installer, and a warning fails it.

- The version is the one of the build ([Release](release.md)): the semantic version names the files, the
  file version of `Bravo.exe` is the version of the product.
- The packages of the application and of the library are the ones of their lock files: the application is
  restored once, in locked mode on GitHub Actions, and published without another restore.
- A release signs `Bravo.exe`, the assemblies of the host and the library before a package holds them,
  and each package after it is built and validated. Any other build signs nothing.
- The same project makes the two archives of a release: each is the folder of a runtime, as it is.

**Tests.** Continuous integration builds the packages and then installs them for real, each time in
silence, on a machine of its own that is thrown away: a first installation per machine and per user, the
installed application, the same with policies of the computer that it cannot read
([policies](policies.md)), another build of the same version installed while the application runs, the
public properties given to 1.0.14 and to this version, the upgrade from 1.0.14 with the data of a user and
the version hidden in the title of the window ([startup](startup.md)), a package of the other context, the
packages that carry the runtime, and every removal. Three removals more: one that fails after its files are
removed, which leaves the installation, the data of the user and the registration as they were; one with
rollback disabled, which removes them; and one while a file of the data folder is in use, which removes the
application, leaves that file, and names it in its log. The removal fails by an action that the test adds to
a copy of a package: no package has anything that exists for a test. The dialogs are checked by hand.

- Two builds of one commit are, to Windows Installer, two products with the same version and the same
  files: they differ by the product code and the package code that each build gives a package. The test
  makes the other build from a copy of a package, with two new codes, and installs it over the first:
  what the release of a version does to a preview of it.
- The workflow of a release runs the same tests on its signed packages, and checks that the files of the
  host that a package installed carry the signature of the build.

## In the code

Windows Installer packages install the application
([ADR 0020](../decisions/0020-windows-installer-packages-built-with-wix-which-run-a-program-of-the-host.md));
this document describes them above. What a package cannot do with its tables it asks of
`Bravo.Setup.CA.dll`, the custom action library of `src/Bravo.Setup`
([ADR 0029](../decisions/0029-the-packages-run-a-custom-action-library-on-net-framework-which-writes-its-errors-in-the-log.md)):

| Part | Path |
| --- | --- |
| The entry points of the actions, the composition of the library, and its writes in the log | `CustomActions.cs` |
| What carries out an action: the check of its data, and the errors of a file or a folder | `SetupActions.cs` |
| The runtime that the library asks of Windows Installer | `CustomAction.config` |
| The JSON of the registration and of an event, with the reader and the writer of .NET Framework | `Json.cs` |
| The rule of .NET for a full path, which .NET Framework 4.7.2 does not have | `FullPath.cs` |
| The file that registers the application as an external tool of Power BI Desktop, and its icon | `ExternalTools/` |
| The events of an installation and of a removal, the identifier of who installs, the item of an event and the sink that sends it | `Telemetry/` |
| The removal of the data folder of a user | `UserData/` |

- The library finds no folder by itself: every path is a value of the data of an action. A test gives it a
  folder of its own, under the temporary folder of the user, and nothing else to write in. The one name
  that the library holds is the one of the data folder (`UserDataFolder.Name`): `RemoveUserData` is given
  the folder of local application data, and removes that subfolder alone.
- What sends is behind an interface, `IInstallationEventSink`. The registration and the data folder are
  static classes, which a test calls with folders of its own.
- Only a build with `-p:BravoReleaseBuild=true`, the one of the packages, defines `BRAVO_RELEASE_BUILD`,
  with which `CustomActions` composes the sink that sends. Any other build, the one of the tests included,
  composes the sink that sends nothing. The connection string is a constant of the library,
  `TelemetryDestination.ReleaseConnectionString`: a test of the architecture checks that it is the one of the
  application, and writes neither.
- The library is on .NET Framework 4.7.2 and C# 7.3, the version of that framework, with no nullable
  reference types. It references no package but DTF and uses no reflection: an assembly of another package
  could need a binding redirect, which only `CustomAction.config` could hold. The clock is
  `DateTimeOffset.UtcNow`, read in `HttpInstallationEventSink` alone, under a `#pragma` that says why: .NET
  Framework has no `TimeProvider`. The rest takes the time from who calls it, and a test passes its own.
- A new action is a method with `[CustomAction]` in `CustomActions`, a method of `SetupActions` that names the
  keys of its data, and a `SetProperty` and a `CustomAction` of the same name in `Package.wxs`.
- An action writes its message in the log through a record whose format is `[1]`: the text of a message,
  such as a path, is not read as the name of a property.
- An action never throws for a file or a folder that cannot be written or removed: `SetupActions.Run`
  catches `IOException` and `UnauthorizedAccessException`, writes the message in the log, and returns
  failure, which the packages ignore. Any other exception, of the composition as well, reaches DTF, which
  writes it in the log and returns failure.
- `SetupActionsTests` run each action with the data that `Package.wxs` gives it, and read the lines that it
  writes in the log. The tests run on the .NET Framework of the machine, which a runner has in version 4.8.x.
  Only the packages that continuous integration installs run the library in Windows Installer. The tests of
  the sink that sends post to `TestIngestion`, on the loopback address ([telemetry](telemetry.md)).

The packages are one WiX project, `installer/Bravo.Installer.wixproj`, on the WiX SDK 5.0.2 that its first
line names. It is not a project of the solution: a build of the solution makes no package.

| Part | File of `installer/` |
| --- | --- |
| Which package a build makes, its file name, the validation rules that are turned off and why, and the targets that a build is asked for | `Bravo.Installer.wixproj` |
| The folder of the application as a package installs it, which is its publish and the files of the page, the build of the library, and the archive of a folder | `Stage.targets` |
| What signs a file, and with which certificate | `Sign-Files.ps1` |
| What checks the signature of a file: after it is signed, and on the files that a package installed | `Confirm-Signature.ps1` |
| The version that the packages of a commit get, read without building them | `Get-BuildVersion.ps1` |
| The package: identity, upgrade, choices, folders, files, registry values, shortcuts, what it runs and when | `Package.wxs` |
| The dialog set, which is the one of WiX with two dialogs replaced | `Dialogs.wxs` |
| The texts | `Package.en-us.wxl` |
| The licence and the two images of the dialogs | `assets/` |
| The real installations that continuous integration runs | `tests/Test-Packages.ps1` |

- A build is asked for a target, which reads the version of the build and builds the project again with
  it: `BuildPackage` for the one package that `BravoScope` and `BravoSelfContained` name, `BuildPackages`
  for the four, `BuildRelease` for the six files, which are the four packages and the two archives. The
  version is computed in a target, after the names of the files are fixed: a build that is not given it
  stops and says so. `ReadBuildVersion` is the one place that says which of the versions that the package
  computes names the files, and which is the version of the product.
- Before a package is built, `web/` has made the release bundle: `npm ci`, then `npm run build:Release`. The
  build of the installer never runs npm, and stops with these two commands when the bundle is missing.
- Every action of `Package.wxs` loads the library from the `Binary` table of the package: the library is not
  installed. Each action has its data written by a `SetProperty` of its own name just before it. What an
  action does that cannot be taken back is a commit action
  (`Execute="commit"`), under the condition `NOT RollbackDisabled`, with a deferred twin under
  `RollbackDisabled` before `InstallFinalize`: Windows Installer runs no commit action when rollback is
  disabled.
- `Package.wxs` is one source for the two contexts: the build defines `Scope`, and what differs is inside
  `<?if?>`. Its public properties, registry values, folders and product names are the ones of 1.0.x, which
  this document lists above.
- `Stage.targets` publishes `Bravo.Desktop` with `dotnet publish` for `win-x64` into the layout of its
  runtime, and builds `Bravo.Setup` with `dotnet build` into `artifacts/installer-stage/setup/`, where DTF
  writes `Bravo.Setup.CA.dll`. The library is built for any processor: the stub of DTF is a 32-bit one, and
  a runtime identifier would put a 64-bit assembly in it.
- `Bravo.Desktop.csproj` declares `win-x64`, so its lock file holds the packages of that publish: the
  application is restored once, in locked mode on GitHub Actions as every restore, and published with
  `--no-restore`. A restore of the application downloads the three runtime packs of `win-x64`. The library
  is restored by its build, from its lock file.
- A layout is made again when a file of `src/`, of the page, of `policies/` or of the build rules is newer
  than it, when one was added or deleted, and when the build is of another commit, or asks for another
  version or another signature: the names of those files and what was asked are kept beside the layout. The
  application has the version and the commit of its build in itself; the library is built again for its
  files and its signature alone. The two packages of one runtime hold the same files: the second one
  takes the cabinet that the first one compressed, kept beside the layout and removed with it.
- `BravoSigning` says what a build signs with, and `Sign-Files.ps1` what each value needs: `KeyVault`, the
  certificate of the publisher, for a release; `TestCertificate`, a certificate of the current user, for a
  run that publishes nothing. `Stage.targets` signs `Bravo.Setup.CA.dll`, and in a layout `Bravo.exe` and
  every `Bravo*.dll`, as part of making them: a new assembly of the host is signed by its name. The library
  is signed after DTF has packed it, and the signature covers the assemblies packed in it: the stub of DTF
  finds them by reading its own file, which the signature does not change but at its end. The package is
  signed by the target `SignMsi` of the WiX SDK, after the validation and before the copy to
  `artifacts/installer`. Without `BravoSigning` nothing is signed.
- The archive of a layout is written beside the packages, outside the layout: everything in a layout is
  installed.
- A package is the release build of the application and of the library: `Stage.targets` passes
  `-p:BravoReleaseBuild=true` to both.
  The `Bravo.exe` of a layout is therefore the same instance as an installed Bravo, with the same data
  folder.
- The build validates a package against the rules of Windows Installer (ICE), also on a developer's
  machine, and a warning fails it. `Bravo.Installer.wixproj` says why each rule that is turned off is.
- A package is validated where the build writes it, under `artifacts/obj`, at a path that is the root of the
  repository and up to 143 characters more: the longest is the one of the package per user that uses the
  installed runtime, in a build that is not a public release. When that path is too long the build stops
  with `WIX0301`, "Failed to open the database", which does not name the cause. Measured with a root of 110
  characters: a path of 253 characters fails, and the one of a public release, 241, does not. A clone in a
  deep folder, such as a temporary one, needs a shorter root.
- `Dialogs.wxs` is derived from three files of WiX, under their licence: what differs from them is marked.
- `tests/Test-Packages.ps1` installs and removes Bravo on the machine that runs it, so it refuses to run
  outside GitHub Actions: a package is installed on a runner, or on a machine that can be thrown away. What
  a test needs of a package that the package does not have, such as a removal that fails, it adds to a copy
  of the package (`New-PackageWithFailingRemoval`): nothing in `Package.wxs` exists for a test.
- A package is a release build, whose library and application send to the telemetry of the product.
  `tests/Test-Packages.ps1` gives every installation `INSTALLERTELEMETRYENABLED=0`, and reads in the tables
  of each package that a first installation without it checks the box all the same, as in 1.0.x. For the
  application it writes the policy of the computer `TelemetryEnabled` = 0 before its first installation,
  keeps it through the test of the policies that cannot be read, and removes it at its end. After each run
  of the application it checks that the data folder holds no telemetry waiting to be sent, and after each
  run that opens the window that its log says that the policy turned telemetry off
  ([telemetry](telemetry.md)).
- The tests take the semantic version and the file version of the build, and no version of their own.
  `New-OtherBuild` gives a copy of a package another product code and another package code, as
  `New-PackageWithFailingRemoval` adds its action to a copy: both go through `New-ChangedPackage`.
