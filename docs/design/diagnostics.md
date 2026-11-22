# Diagnostics and logging

The host keeps a log in memory and in one file for each run, and writes its warnings and errors also to the
Event Log of Windows ([ADR 0026](../decisions/0026-a-log-in-memory-and-in-one-file-per-run-warnings-also-in-the-event-log.md)).
An exception that nothing foresaw ends the process with a report beside the log, which Bravo shows started as
its reporter ([ADR 0027](../decisions/0027-a-crash-leaves-a-report-beside-the-log-shown-by-bravo-started-as-its-reporter.md)).
Telemetry is backlog B-9.

**Where a record goes.** Every record goes to the first two places, a warning or an error to all three.

| Place | What it keeps |
| --- | --- |
| Memory | The last 1000 records, which the diagnostics pane of the page reads through `GET /log` ([API](api.md)) |
| A file for each run | `Logs\Bravo-<start>-<process>.log` in the data folder ([startup](startup.md)), the start in UTC as `yyyyMMdd-HHmmss`. The 10 latest runs are kept: a start deletes the oldest files. At 5 MB a last line says that the file reached its limit, and the rest of the run is not written to it |
| The Application log of Windows | Warnings and errors, under the source `.NET Runtime` with the event ID 1000, which every Windows with .NET has: Bravo registers no source of its own. A development build writes nothing there |

- Each record is in the file before the call that wrote it returns: a process that ends at once leaves its
  last records in the file. While Bravo writes the file, a program that lets it go on writing can read it, as
  `type`, `findstr` and `Get-Content` do; one that does not, such as `File.ReadAllText` of .NET, cannot.
- A file that cannot be created is a warning, and the run goes on without one. A write that fails closes the
  file for the rest of the run. Nothing that the Event Log answers reaches the code that wrote the record.

**Levels.** The categories of Bravo, the types under `Bravo`, are recorded from Information; the libraries from
Warning, since their information tells of every request with its address. The diagnostic level of the options
does not change what is recorded: it chooses what the pane shows. At Basic the pane shows the warnings and the
errors of the host, at Verbose also its information, at None nothing.

**What is recorded.**

- The start, after the arbitration: the version, whether the build is a development build, the runtime and
  whether the build carries it, the architecture of the process, the system, the file of the log. The end,
  with the exit code.
- The version of the WebView2 runtime, each time the webview creates its engine: at the start, and at a
  recovery that creates a new control ([window and webview](window-and-webview.md)).
- A process of the webview that failed, with its kind, its reason and its exit code: a warning when the page or
  the browser ended, information for the other processes, which the engine starts again. Each recovery, as
  information; the end of the application because of its webview, as an error, before its message.
- A crash, as critical, with the path of its report (below). A task that failed and that nothing awaited, as an
  error, when the runtime finds it after a collection of the memory: the process goes on.
- A key of the policies that cannot be read, as an error, before the message and the exit code 5
  ([policies](policies.md)).
- An error that a use case foresaw, as a warning of `BravoExceptionHandler`: the method, the path without the
  query, the code and the exception with its causes, such as what .NET reported of a request that did not reach
  its server ([network](network.md)). A defect of the host, the answer 500, as an error of ASP.NET Core with its
  exception.
- The warnings of the components: a settings file that cannot be read or written ([settings](settings.md)), a
  pipe of the single instance that cannot be created ([startup](startup.md)), credentials of the proxy server
  that cannot be read ([network](network.md)).

**Trace.** A record carries the trace of the work that it belongs to: the W3C trace ID of the current activity,
32 hexadecimal digits.

- In a request it is the trace of the request. The `traceId` of an error answer, `00-<trace>-<span>-<flags>`,
  has it as its second part, and the page shows it with the error.
- Outside a request Bravo starts an activity for each piece of its own work: the start, until the host is
  built; each connection to the pipe of the single instance; the reading of the settings file; the creation of
  the engine of the webview. A record outside any activity, such as one of a library while the host stops, has
  no trace.
- Telemetry sends the trace of the record (backlog B-9). A request of the host to a server carries no header of
  the trace ([network](network.md)).

**The file.** UTF-8, the line ends of Windows, one line for each record, with its time in UTC, its level, its
trace or `-`, its category with the event ID, and its message:

```text
2026-11-22T09:30:15.123Z Warning 4bf92f3577b34da6a3ce929d0e0e4736 Bravo.Api.Problems.BravoExceptionHandler[1] The request GET /releases/latest ended with the error UpdateCheckFailed.
    Bravo.Core.Errors.BravoException: The update feed cannot be read.
```

The other lines of a message and the exception follow, each indented by four spaces.

**Who reads it.**

- The user, in the diagnostics pane: a record of the host is `[HOST] <type>.<event>`, with the message, the
  trace and the exception, and a warning or an error is marked as an error. The pane reads the new records
  every 5 seconds while its level is not None; the first reading gets every record that the host still keeps.
  The page writes its own messages only in the pane: they never reach the host. An error that the code of the
  page throws, or a promise that it rejects, and that nothing catches, is shown with its stack at every level.
- Support, in the file: `%LOCALAPPDATA%\Bravo\Logs` for an installed or a portable copy, with the reports of the
  crashes. The trace of an error that the user reports leads to its record. In the Event Viewer, under Windows
  Logs, Application: the text of an entry begins with the category, the event ID, the trace and the scopes of
  the request, such as its path.
- Bravo sends the log and the reports nowhere. A company may collect the Event Log with its own tools.

**Crashes.** An exception that nothing foresaw, on any thread, in the host or in its start, ends the process.

- One handler takes the first one, installed once Windows Forms is configured ([startup](startup.md)).
  Another thread whose exception escapes meanwhile waits for the end of the process; a handler that fails ends
  it with `FailFast`.
- The handler writes the report and the critical record, with one trace: the one of the work that failed, or
  one of its own. It starts `Bravo.exe --crash-report <report>` and returns. The runtime then ends the process
  with its own exit code, its entry in the Event Log and Windows Error Reporting, as for any .NET application.
- A service of the background that fails stops the host, and the window closes. Once the host has stopped, its
  exception is thrown again, and ends the process as above. A service that fails while the host starts cancels
  the start of the services after it: its exception is thrown, not that cancellation.
- Before the data folder is known, when the report cannot be created or written, whatever the cause, which is
  a warning, and when the reporter does not start, the process that crashes shows a message: the type and the
  message of the exception, and where the report is when there is one.
- A stack overflow, `FailFast`, a crash of native code and a hang leave no report, and no end in the log of the
  run: the Event Log of Windows and Windows Error Reporting have them (backlog B-43).

**The report.** `Logs\Bravo-<start>-<process>.crash.json`, the name of the file of the log of the run with
another extension; a crash before the log has the time of the crash in its name. The 10 latest are kept, apart
from the files of the log. UTF-8 JSON, indented:

| Member | What it holds |
| --- | --- |
| `format` | 1. A change that a reader of an earlier report would misread is a new format |
| `version` | The version of the build, as the user reads it |
| `time` | When the exception was caught |
| `trace` | The trace of the critical record |
| `logFile` | The name of the file of the log of the run, in the same folder, or `null` |
| `exception` | `type`, its full name; `message`; `text`, the lines of what .NET writes of the exception: its causes and their stacks, with their lines, since the packages install the symbols ([installer](installer.md)) |

The texts lose the folder of the profile, the name of the user and the names of the computer, for Windows and
on the network, which become `%USERPROFILE%`, `%USERNAME%` and `%COMPUTERNAME%`; a name and a folder only as a
whole word. They are read at the crash, not at every start. The report never holds the command line, the
environment, the settings, credentials or the memory of the process.

**The reporter.** `Bravo.exe --crash-report <report>`, these two arguments alone. It takes no instance, opens no
log, reads no policy and builds no host: it shows a task dialog and ends with exit code 0. The dialog follows
the Windows guidelines for an error message
([error messages](https://learn.microsoft.com/windows/win32/uxguide/mess-error)): the icon of Bravo with the
overlay of an error, which they allow when the subject has an icon of its own; the heading "Bravo encountered
an error and closed unexpectedly"; the technical details behind Show details; Close as the default button. One
departure: the text keeps in view the message and the short type of the exception, the version, the time in UTC
to the second and the trace ID, because a screenshot of the dialog is what a user sends; the trace finds the run
in the log. Show details has the first 20 lines of the text of the exception, the file of the log and the
report. A dialog does not scroll: the text shows the first 3 lines of the message, and a line longer than 300
characters is cut, with an ellipsis; Copy details has everything. The footnote says that nothing has been sent,
and then whether Copy details worked.

| Button | What it does |
| --- | --- |
| Report on GitHub | Opens a new issue of the repository, `https://github.com/sql-bi/Bravo/issues/new`, with no parameter: no template and no field of a form is named, so a change of the templates cannot break it |
| Copy details | The version, the time, the trace, the file of the log and the report, then the text of the exception |
| Open folder | Explorer, with the report selected |
| Close | Ends the reporter |

The three buttons leave the dialog open. A report that cannot be read, or that lacks a member that must not be
`null`, gets a dialog with the same icon and heading, and its path. A crash of the reporter is the message of the
process that crashes.

**Never recorded.** The token of the API, passwords and credentials, the bodies and the headers of requests,
and the query of an address. A record may name a path in the profile of the user, such as the one of the
settings file.

## In the code

| Part | Path |
| --- | --- |
| The records that the pane reads, the types that travel, the route | `src/Bravo.Api/Diagnostics/` |
| The record of an error that a use case foresaw | `src/Bravo.Api/Problems/BravoExceptionHandler.cs` |
| The file of the log of a run: its limit and its writes | `src/Bravo.Platform.Windows/Diagnostics/LogFile.cs` |
| The names of the files of a run and the runs kept, for the log and the reports | `src/Bravo.Platform.Windows/Diagnostics/RunFiles.cs` |
| The handler of an exception that escapes, and what it does: the report, its file, the names that it loses, the record, the start of the reporter, the message of the process that crashes | `LastChanceExceptionHandling.cs`, `CrashReporting.cs`, `CrashReport.cs`, `CrashReportFile.cs`, `PersonalNames.cs`, `CrashReporterProcess.cs`, `FatalExceptionDialog.cs` in `src/Bravo.Desktop/Diagnostics/` |
| The reporter: its command line, its dialog, the address of the issues | `CrashReporter.cs`, `CrashReportDialog.cs` in `src/Bravo.Desktop/Diagnostics/` |
| The record of a task that nothing awaited | `src/Bravo.Desktop/Diagnostics/UnobservedTaskExceptionLog.cs` |
| The exception of a service of the background, thrown again after the host stopped or in place of the cancellation of its start | `DesktopHost.ThrowIfAServiceFailed` and `DesktopHost.Start` in `src/Bravo.Desktop/Hosting/DesktopHost.cs` |
| The crash that a development build is asked for | `src/Bravo.Desktop/Development/DevelopmentCrashTrigger.cs` |
| The log of the process: the levels, the record and its trace, the line of the file, the guard of the Event Log | `src/Bravo.Desktop/Diagnostics/` |
| The log opened after the arbitration, the records of the start and of the end | `src/Bravo.Desktop/Program.cs` |
| The pane, which reads the records of the host and shows the errors of the page | `web/controllers/logger.ts` |

- `Main` opens the log after the arbitration, so a second process writes nothing, and closes it after the
  host. `DesktopHost.Build` gets it as an instance ([ADR 0004](../decisions/0004-bootstrap-services-outside-the-container.md))
  and registers its factory and its buffer, which `GET /log` reads. `AddApi` adds no buffer: the tests of the
  API add one with their use cases.
- A record of Bravo is a `[LoggerMessage]` method in the `<Type>.Log.cs` file of its type, with an event ID of
  its own in that type. A use case does not record the errors that it foresees: the handler records each one.
- Work of Bravo that runs outside a request and can record starts an `Activity` of its own, and stops it when
  the work ends: an activity left current would give its trace to what follows.
- ASP.NET Core starts the activity of a request only when a logger is enabled for its category at Critical, or
  something listens: without it, the `traceId` of a problem is the identifier of the connection.
  `DesktopHostApiTests` fails then.
- The providers of the factory, which it does not dispose, are disposed by `HostLog`, after the factory.
- The provider of the Event Log is wrapped in `GuardedLoggerProvider`, which keeps its exceptions and passes it
  the scopes of the factory: they carry the trace, which the factory adds with
  `ActivityTrackingOptions.TraceId`.
- `Main` installs the handler of an exception that escapes once Windows Forms is configured. The handler
  learns the data folder once the command line is read, and the log once it is open; `Main` detaches the log
  before it closes it. Until the data folder is known, the handler can only show its message. No service of
  the host gets the handler.
- On .NET 10 a host that a failed service stopped ends as if nothing had failed:
  `DesktopHost.ThrowIfAServiceFailed` throws the exception again, or an `AggregateException` when several services
  failed. .NET 11 does the same from `StopAsync` itself
  ([breaking change](https://learn.microsoft.com/dotnet/core/compatibility/extensions/11/ihost-runasync-stopasync-throw-backgroundservice-failure)),
  and the call after `StopAsync` goes with the move to .NET 11. The breaking change says nothing of the start:
  `DesktopHost.Start` keeps the method until a test shows that .NET throws the exception of the service there too.
- `--dev-crash=<place>`, in a development build, throws an exception that nothing catches: while the host starts
  (`start`), on the thread of the window once it is shown (`window`), on a thread of its own (`thread`), in a
  service of the background once the window is shown (`service`), or in a task that nothing awaits
  (`unobserved`).
- The unit tests prove each path of the handler without ending a process, with a reporter and a message of the
  test. `BravoCrashTests` starts the executable with `--dev-crash` at each place that ends it, and checks its
  exit code, the report, the critical record with the same trace, and the dialog of the reporter, which it
  closes. These run on a runner of GitHub Actions only: each crash leaves a report of Windows Error Reporting on
  the computer. The process of the tests sets the error mode `SEM_NOGPFAULTERRORBOX`, which the processes that
  it starts inherit, so that no dialog of Windows waits. The reporter alone, with a report and with one that
  cannot be read, and the task that nothing awaits run everywhere.
- What no test sees is checked by hand: a crash of an installed build, the buttons of the dialog, the issue in
  the browser, the entry of the runtime in the Event Log.
- The tests open the log in a temporary data folder, without the Event Log. The one test that writes to the
  Event Log runs on a runner of GitHub Actions only, and finds its entry by a text of its own.
  `Log_SecretsThatRequestsCarry_AreNeverRecorded` sends a value in the place of the token, in the query and in
  the address of a proxy, and finds it in no record.
