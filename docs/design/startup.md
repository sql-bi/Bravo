# Startup, lifetime and single instance

`Main` runs six steps in a fixed order, on one STA thread, and returns an exit code
([ADR 0013](../decisions/0013-startup-in-a-fixed-order-one-instance-by-mutex-and-pipe.md)):

| Step | What happens |
| --- | --- |
| 1 | Windows Forms is configured; the DPI mode, per monitor, is a property of the project |
| 2 | An exception that escapes, on any thread, reaches one handler, and the runtime ends the process. Until backlog B-8 the handler shows a message |
| 3 | The command line becomes an activation request. The process becomes the owner of the instance, or hands the request over to the owner and ends |
| 4 | The log of the process is opened, and the start recorded ([Diagnostics](diagnostics.md)). The policies are read ([Policies](policies.md)); a key that cannot be read is recorded, releases the ownership and ends the start with a message. The host is built and started. It gets as instances the handler of step 2, the ownership of step 3, the policies and the log |
| 5 | The main window, a service of the host, runs the message loop |
| 6 | The end is recorded with its exit code. The host is stopped and disposed, then the log is closed and the ownership released |

Up to step 3 the application reads and writes nothing of its own, on the disk or in the registry, and a second
process creates no host and no window.

**Host.** It is the host of a web application, built empty: no configuration file, no environment variable, no
logging provider and no server arrives with the builder, and every registration is validated when the host is
built. Its logging is the log that `Main` opened ([Diagnostics](diagnostics.md)). The one server it has is the
one of the [API](api.md). Its lifetime is the main window, not a console: closing the window stops the host, and
a host that stops on its own closes the window. A stop takes at most 5 seconds, the time Windows gives an
application when a session ends. A component registers its services in an extension method of
`IServiceCollection`, which only `Bravo.Desktop` calls.

**A request to end.** Windows asks an application to end when the session of the user ends, and Restart
Manager asks it when an installation must replace its files
([guidelines](https://learn.microsoft.com/windows/win32/rstmgr/guidelines-for-applications)): the window is asked
whether it can end, then told to. The application always answers yes, closes its window as when the user
closes it, and ends with exit code 0 within the time of a stop. It does not ask Restart Manager to start it
again.

**Single instance.** One instance for each user in each Windows session, at every privilege level.

- Ownership is a named mutex, scoped to the user and to the session. A process is the owner only when it has
  acquired the mutex; a mutex left by an owner that ended without releasing it is acquired.
- Hand-over is a named pipe in the same scope. A request is the version of the protocol, the length of the
  payload and the payload, a JSON object of at most 1 MiB. The owner answers with one byte: accepted, busy,
  shutting down, or a refusal. The connection and the exchange have a time limit.
- A second process that cannot deliver tells the user why and exits with code 1; it never becomes an owner. It
  tries again for 10 seconds while the owner may be starting, busy or ending. It gives up at once when access
  is denied, as between two privilege levels, and when the owner refuses the request.
- The owner starts even when it cannot create the pipe, and logs a warning.
- Before it sends, the second process grants the owner the right to come to the foreground.
- The names begin with an identifier that is new in 1.1.0: a 1.0.x instance and a 1.1.0 one do not recognize
  each other. Later versions keep the identifier; a change of the messages is a new version of the protocol,
  which an owner of another version refuses.

**Build identity.** A build that is not made for an installer package is a development build
([ADR 0015](../decisions/0015-page-under-a-reserved-host-name-api-connection-in-an-object.md)). The build of the packages
sets a build property, which reaches the application as metadata of its assembly; `Main` reads it once,
before the arbitration.

| | Release build | Development build |
| --- | --- | --- |
| Instance | The identifier of 1.1.0 | An identifier of its own: it runs beside the installed application |
| Data folder, under the local application data of the user | `Bravo` | `Bravo.Development`, or the folder that `--dev-data-folder` names |
| Window title | `Bravo for Power BI - v<version>`, or `Bravo for Power BI` for a user who hid the version | The same, followed by `[development build]` |
| Update feed | The one of the releases | None, unless it is given `--dev-update-feed` ([Updates](updates.md)) |
| Policies | The keys of the policies, of the computer and of the user | None, unless it is given `--dev-policy-key` ([Policies](policies.md)) |

The version is the semantic version of the build, such as `1.1.0` or `1.1.0-beta.1`, which the build
computes and writes in the metadata of the assembly ([Release](release.md)). Outside a public release it
ends with the commit: a development build shows `1.1.0-beta.1.g4497d5d308`. The identity also has the file
version, four numbers, and whether the build carries the runtime.

A user hides the version in the title as in 1.0.x: with the DWORD value `applicationTitleVersionHidden` of
`HKEY_CURRENT_USER\Software\SQLBI\Bravo` set to 1, which the window reads when it is created. Any other value,
or a value of another kind, shows the version. No option and no package writes it.

**Activations.** Every start is an activation request, the first one included. The requests go through one
bounded channel of 16 with a single consumer, which starts when the main window is shown: a request that
arrives earlier waits. A second process is answered busy when the channel is full, and shutting down after the
window was closed. The consumer publishes each request as an event of the API, which the page reads
([events](api.md)), and brings the window to the foreground.

**Command line.** `--server` or `-s`, `--database` or `-d`, and `--ppid`, with the value after a space, `=` or
`:`. The arguments of the external tool registration are unchanged:
`--server="%server%" --database="%database%"`. A start with no arguments is valid. An error never stops the
start: it travels with the request. The request carries what the command line gives; the parent process and
the report name are backlog B-10.

**Exit codes.** An exception that escapes ends the process with the code of the runtime. The application
itself ends with one of these:

| Code | When |
| --- | --- |
| 0 | The application ran and was closed, or a start was handed over |
| 1 | A hand-over failed |
| 2 | The files of the user interface are not beside the executable |
| 3 | The WebView2 runtime is installed and the webview could not be created with it |
| 4 | The processes of the webview ended more times in a row than the application recovers from |
| 5 | A key of the policies exists and cannot be read |
| 1223 | The WebView2 runtime is not installed: the code that 1.0.x ends with |

## In the code

`Program.Main` of `Bravo.Desktop` runs six steps in a fixed order
([ADR 0013](../decisions/0013-startup-in-a-fixed-order-one-instance-by-mutex-and-pipe.md)), which
this document describes above. Where each part lives:

| Part | Path |
| --- | --- |
| The order of the startup | `src/Bravo.Desktop/Program.cs` |
| The host: what it is built with, its lifetime, its stop | `src/Bravo.Desktop/Hosting/` |
| The main window, its colors for a theme, what ties it to the host, what reaches it from another thread, and the exit code that the window ends with | `src/Bravo.Desktop/Shell/` |
| The webview, and everything that knows its engine | `src/Bravo.Desktop/WebView/` ([Window and webview](window-and-webview.md)) |
| The exit codes of the process | `src/Bravo.Desktop/ExitCode.cs` |
| What is done with an exception that escapes, and the log of the process ([Diagnostics](diagnostics.md)) | `src/Bravo.Desktop/Diagnostics/` |
| The command line, and the consumer of the activations | `src/Bravo.Desktop/Activation/` |
| What a development build is asked for a browser or a test: one more allowed origin, the file with the address and the token, the update feed, the data folder, the key of its policies | `src/Bravo.Desktop/Development/` |
| The data folder, chosen once after the arbitration and passed to `DesktopHost.Build`, which has no default for it: a test that builds the host gives it a folder of its own | `src/Bravo.Desktop/DataFolder.cs` |
| The activation request, and the channel that every activation goes through | `src/Bravo.Core/Activation/` |
| The single instance: ownership by mutex, hand-over by pipe, arbitration | `src/Bravo.Platform.Windows/SingleInstance/` |
| The identity of the build: release or development, with its instance identifier, data folder and window title, its versions, and whether it carries the runtime | `src/Bravo.Desktop/BuildIdentity.cs` |
| The choice of the user that hides the version in the title, read from the registry | `src/Bravo.Desktop/Shell/RegistryWindowTitleSettings.cs` |
| The listener as a service of the host, the message of a failed hand-over, the end of a second process | `src/Bravo.Desktop/SingleInstance/` |
| The settings of the user, kept in a file of the data folder ([Settings](settings.md)) | `src/Bravo.Core/Settings/` and `src/Bravo.Platform.Windows/Settings/`, registered in `src/Bravo.Desktop/Settings/` |
| The policies, read once before the host is built ([Policies](policies.md)) | `src/Bravo.Core/Policies/` and `src/Bravo.Platform.Windows/Policies/`, registered in `src/Bravo.Desktop/Policies/` |
| The proxy of the connections, read from the settings when the host starts ([Network](network.md)) | `src/Bravo.Core/Network/` and `src/Bravo.Platform.Windows/Network/`, registered in `src/Bravo.Desktop/Network/` |
| The Windows functions of a project, listed in its `NativeMethods.txt` and wrapped by capability | `Interop/` in `Bravo.Desktop` and in `Bravo.Platform.Windows` |

- A component registers its services in an extension method of `IServiceCollection` beside its code, and
  `DesktopHost.Build` calls it.
- `DesktopHost.Build` is the one place that names the builder, `WebApplication.CreateEmptyBuilder`: what a web
  application has by default, such as configuration files, environment variables, logging providers and a
  server, is in the host only when `Build` adds it. The API is added with `UseLoopbackServer`, `AddApi` and
  `UseApi` ([API](api.md)).
- The ownership of the instance is a mutex, which only the thread that acquired it can release: `Main` acquires
  it and releases it, and a test that acquires one stays on one thread. A test that awaits holds it through
  `OwnedInstance`, on a thread of its own.
- A service never asks the container for the main window, which `Main` creates on its thread. It reaches
  the window through `MainWindowDispatcher`, which has the window while its message loop runs and runs a
  function on its thread, with its handle: a dialog box of Windows that belongs to the window is shown that
  way, as the one of the proxy credentials ([Network](network.md)). Without a window the function does not
  run.
- `DesktopHostApiTests` starts the host as the application builds it, without a window, and calls the API on a
  real socket.
- `Main` reads the identity of the build once, from the metadata of the assembly, and passes it on: no code
  asks again which build it is. A build is a development build unless it was built with
  `-p:BravoReleaseBuild=true`, which only the build of the installer packages does. The property is not the
  configuration: `-c Release` on a developer's machine is a development build. A development build is
  another instance than the installed application, so both can run at once.
- The tests of the single instance use real mutexes and pipes, each test under names of its own.
  `BravoProcessTests` starts the `Bravo.exe` that the build wrote, a development build: it shows a window for
  a moment, and it is skipped when a development build already runs for the user.
- The test classes that start the executable are one collection of xUnit, `BravoExecutable.Collection`: the
  application has one instance, and they run one after the other. `BravoExecutable` starts it with a data
  folder of the test and without the proxy variables of the tests, and waits for its window or for a file
  that it writes.
- No code of the application handles a request to end: Windows Forms answers the two messages, and closes
  the main window at the second one. `BravoProcessTests` sends them as Restart Manager does, and fails when
  the application no longer answers yes or no longer ends with exit code 0.
