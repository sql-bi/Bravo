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

- The files are the application published for `win-x64` with the identity of a release build, and
  `wwwroot`: the static files of the page with the release bundle.
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
`Bravo.Setup.exe`, a program of the host that the package carries in itself and does not install: a command
line, no window, and no need of the .NET runtime, so that it runs where the application could not start.

| Command | Options | What it does |
| --- | --- | --- |
| `register-external-tool` | `--tool-file`, `--executable` | Writes the file that registers the application as an external tool of Power BI Desktop, with its folder when it is missing. A file that is there is replaced |
| `unregister-external-tool` | `--tool-file`, `--executable` | Removes that file when it registers the executable. A file that registers another executable, such as one written by hand for another installation, or that is not a registration, stays. The folder stays: other tools register in it |
| `report-install`, `report-uninstall` | `--scope`, `--product-name`, `--product-version`, `--product-build`, `--telemetry-enabled` | Reports the event `Install` or `Uninstall`, when the value of `--telemetry-enabled` is `1` |
| `remove-user-data` | `--local-application-data` | Removes the data folder of the application, `Bravo`, with everything in it, from the folder of local application data that it is given, and nothing else of that folder |

| When | Per machine | Per user |
| --- | --- | --- |
| An installation, also over an earlier version, after its files are copied | `register-external-tool` | |
| A first installation that fails and is undone | `unregister-external-tool` | |
| An installation, also over an earlier version, that has succeeded | `report-install` | `report-install` |
| A removal that is not part of an upgrade and has succeeded | `unregister-external-tool`, `remove-user-data`, `report-uninstall` | `remove-user-data`, `report-uninstall` |

- An option is `--name value`. A command gets each of its options once, and no other; a path is a full path.
- The program finds no folder by itself and reads nothing from the registry: the package passes every
  path, and a test passes folders of its own. One name is in the program and not in the package, the one
  of the data folder: no command line makes the program remove another folder.
- The registration is the one of 1.0.x: the name `Bravo`, the arguments
  `--server="%server%" --database="%database%"`, the same icon, and the full path of the executable. Its
  description is `Bravo for Power BI`. The file is `bravo.pbitool.json`, in the folder that Power BI
  Desktop reads, under the 32-bit Common Files: the package writes there with the rights of the
  installation.
- Every command starts the copy of the program that the package holds in itself, whoever it runs as:
  nothing that an installation or a removal runs is a file of a folder that somebody could have written in.
  Only the registration and its removal run with the rights of the installation.
- What cannot be taken back is done when nothing can fail any more: the removal of the registration, the
  removal of the data of the user and the two events are
  [commit actions](https://learn.microsoft.com/windows/win32/msi/commit-custom-actions), which Windows
  Installer runs after the installation or the removal has succeeded. A removal that fails and is undone
  leaves the data of the user and the registration as they were.
- Where rollback is disabled, by the property `DISABLEROLLBACK` or by the policy, Windows Installer undoes
  nothing and runs no commit action: the package then runs the same commands as the last steps of the
  installation or of the removal.
- An event carries what 1.0.x sends with it: its name, the scope, the name, the version and the build of the
  product, the operating system, and an identifier of who installs, the SHA-256 of the names of the machine
  and of the account that the program runs as. It names nobody, and is computed as 1.0.x computes it, so
  that the counts of the two versions can be compared. A package reports, and removes the data of the
  user, as the account that installs or removes. Where a deployment tool installs in the system context,
  that account is the system: the identifier of an event is then one for the machine. Nothing sends the
  events yet (backlog B-9).
- The exit code is 0 when the command was carried out, also when it found nothing to do; 1 when a file or
  a folder could not be written or removed; 2 when the command line is not valid; 3 for an exception that
  nothing expected, which is written as one line on the standard error and never ends the process with an
  error report of Windows. A package ignores the code: no command fails an installation, and Windows
  Installer writes the code in its log.

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
`win-x64` and the files of the page. The program is published apart and compiled ahead of time, to one file
of about 1 MB, which needs the C++ build tools of Visual Studio on the machine that builds. The build
validates each package against the rules of Windows Installer, and a warning fails it.

- The version is the one of the build ([Release](release.md)): the semantic version names the files, the
  file version of `Bravo.exe` is the version of the product.
- The packages of the application are the ones of its lock file: the application is restored once, in
  locked mode on GitHub Actions, and published without another restore. The packages that compile the
  program ahead of time are in no lock file: the .NET SDK adds them in its own version, as it adds the .NET
  runtime that a package carries.
- A release signs `Bravo.exe`, the assemblies of the host and the program before a package holds them,
  and each package after it is built and validated. Any other build signs nothing.
- The same project makes the two archives of a release: each is the folder of a runtime, as it is.

**Tests.** Continuous integration builds the packages and then installs them for real, each time in
silence, on a machine of its own that is thrown away: a first installation per machine and per user, the
installed application, another build of the same version installed while the application runs, the public
properties given to 1.0.14 and to this version, the upgrade from 1.0.14 with the data of a user and the
version hidden in the title of the window ([startup](startup.md)), a package
of the other context, the packages that carry the runtime, and every removal. Two removals more: one that
fails after its files are removed, which leaves the installation, the data of the user and the registration
as they were, and one with rollback disabled, which removes them. The removal fails by an action that the
test adds to a copy of a package: no package has anything that exists for a test. The dialogs are checked
by hand.

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
`Bravo.Setup.exe`, the program of `src/Bravo.Setup`:

| Part | Path |
| --- | --- |
| The composition of the program, its last defense against an exception, its exit codes, and what carries out a command | `Program.cs`, `ProcessBoundary.cs`, `ExitCode.cs`, `SetupApplication.cs` |
| The command line: the commands, their options, and the reader of the options | `CommandLine/` |
| The file that registers the application as an external tool of Power BI Desktop, and its icon | `ExternalTools/` |
| The events of an installation and of a removal, the identifier of who installs, where the events go | `Telemetry/` |
| The removal of the data folder of a user | `UserData/` |

- The program finds no folder by itself: every path is an option of its command line. A test gives it a
  folder of its own, under the temporary folder of the user, and nothing else to write in. The one name
  that the program holds is the one of the data folder (`UserDataFolder.Name`): `remove-user-data` is
  given the folder of local application data, and removes that subfolder alone.
- What touches the disk or the network is behind an interface: `IExternalToolRegistration`,
  `IUserDataFolder`, `IInstallationEventSink`. The sink that `Program` composes sends nothing (backlog
  B-9).
- The program has no package reference and uses no reflection: the packages carry it compiled ahead of
  time. A new command is a record derived from `SetupCommand`, a name and a reader in `SetupCommandLine`,
  and a case in `SetupApplication.Execute`.
- A command never throws for a file that cannot be written: `SetupApplication.Run` answers with an exit
  code, which the packages ignore. Any other exception, of the composition as well, stops at
  `ProcessBoundary.Run`, the one place that catches every exception: it becomes exit code 3.
- `SetupProcessTests` starts the `Bravo.Setup.exe` that the build wrote, as a package does.

The packages are one WiX project, `installer/Bravo.Installer.wixproj`, on the WiX SDK 5.0.2 that its first
line names. It is not a project of the solution: a build of the solution makes no package.

| Part | File of `installer/` |
| --- | --- |
| Which package a build makes, its file name, the validation rules that are turned off and why, and the targets that a build is asked for | `Bravo.Installer.wixproj` |
| The folder of the application as a package installs it, which is its publish and the files of the page, the publish of the program, and the archive of a folder | `Stage.targets` |
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
- Every action of `Package.wxs` that runs the program starts it from the `Binary` table of the package: the
  program is not installed. What an action does that cannot be taken back is a commit action
  (`Execute="commit"`), under the condition `NOT RollbackDisabled`, with a deferred twin under
  `RollbackDisabled` before `InstallFinalize`: Windows Installer runs no commit action when rollback is
  disabled.
- `Package.wxs` is one source for the two contexts: the build defines `Scope`, and what differs is inside
  `<?if?>`. Its public properties, registry values, folders and product names are the ones of 1.0.x, which
  this document lists above.
- `Stage.targets` publishes `Bravo.Desktop` and `Bravo.Setup` with `dotnet publish` for `win-x64`, into
  `artifacts/installer-stage/`: the application into the layout of its runtime, the program into a folder
  of its own.
- `Bravo.Desktop.csproj` declares `win-x64`, so its lock file holds the packages of that publish: the
  application is restored once, in locked mode on GitHub Actions as every restore, and published with
  `--no-restore`. A restore of the application downloads the three runtime packs of `win-x64`. The publish
  of the program reads and writes no lock file: `PublishAot` adds packages of the version of the SDK, which
  a lock file would hold for one patch of the SDK alone.
- A layout is made again when a file of `src/`, of the page or of the build rules is newer than it, when
  one was added or deleted, and when the build is of another commit, or asks for another version or
  another signature: the names of those files and what was asked are kept beside the layout. The
  application has the version and the commit of its build in itself; the program is published again for
  its files and its signature alone. The two packages of one runtime hold the same files: the second one
  takes the cabinet that the first one compressed, kept beside the layout and removed with it.
- `BravoSigning` says what a build signs with, and `Sign-Files.ps1` what each value needs: `KeyVault`, the
  certificate of the publisher, for a release; `TestCertificate`, a certificate of the current user, for a
  run that publishes nothing. `Stage.targets` signs the program, and in a layout `Bravo.exe` and every
  `Bravo*.dll`, as part of making them: a new assembly of the host is signed by its name. The package is
  signed by the target `SignMsi` of the WiX SDK, after the validation and before the copy to
  `artifacts/installer`. Without `BravoSigning` nothing is signed.
- The archive of a layout is written beside the packages, outside the layout: everything in a layout is
  installed.
- `Bravo.Setup.exe` is published with `PublishAot`, which needs the "Desktop development with C++" workload
  of Visual Studio: the runner has it. Without it the publish fails with "Platform linker not found";
  `-p:BravoSetupNativeAot=false` then makes packages to look at, with the program as one file that carries
  the .NET runtime, about 11 MB. Those are not the packages of a release.
- A package is the release build of the application: `Stage.targets` passes `-p:BravoReleaseBuild=true`.
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
- The tests take the semantic version and the file version of the build, and no version of their own.
  `New-OtherBuild` gives a copy of a package another product code and another package code, as
  `New-PackageWithFailingRemoval` adds its action to a copy: both go through `New-ChangedPackage`.
