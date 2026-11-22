# Diagnostics and logging

The host keeps a log in memory and in one file for each run, and writes its warnings and errors also to the
Event Log of Windows ([ADR 0026](../decisions/0026-a-log-in-memory-and-in-one-file-per-run-warnings-also-in-the-event-log.md)).
What escapes as an exception and ends the process is [startup](startup.md) and backlog B-8; telemetry is
backlog B-9.

**Where a record goes.** Every record goes to the first two places, a warning or an error to all three.

| Place | What it keeps |
| --- | --- |
| Memory | The last 1000 records, which the diagnostics pane of the page reads through `GET /log` ([API](api.md)) |
| A file for each run | `Logs\Bravo-<start>-<process>.log` in the data folder ([startup](startup.md)), the start in UTC as `yyyyMMdd-HHmmss`. The 10 latest runs are kept: a start deletes the oldest files. At 5 MB a last line says that the file reached its limit, and the rest of the run is not written to it |
| The Application log of Windows | Warnings and errors, under the source `.NET Runtime` with the event ID 1000, which every Windows with .NET has: Bravo registers no source of its own. A development build writes nothing there |

- Each record is in the file before the call that wrote it returns: a process that ends at once leaves its
  last records in the file. Any program can read the file while Bravo writes it.
- A file that cannot be created is a warning, and the run goes on without one. A write that fails closes the
  file for the rest of the run. Nothing that the Event Log answers reaches the code that wrote the record.

**Levels.** The categories of Bravo, the types under `Bravo`, are recorded from Information; the libraries from
Warning, since their information tells of every request with its address. The diagnostic level of the options
does not change what is recorded.

**What is recorded.**

- The start, after the arbitration: the version, whether the build is a development build, the runtime and
  whether the build carries it, the architecture of the process, the system, the file of the log. The end,
  with the exit code.
- The version of the WebView2 runtime, each time the webview creates its engine: at the start, and at a
  recovery that creates a new control ([window and webview](window-and-webview.md)).
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
  built; each connection to the pipe of the single instance; the reading of the settings file. A record
  outside any activity, such as one of a library while the host stops, has no trace.
- Telemetry sends the trace of the record (backlog B-9).

**The file.** UTF-8, the line ends of Windows, one line for each record, with its time in UTC, its level, its
trace or `-`, its category with the event ID, and its message:

```text
2026-11-22T09:30:15.123Z Warning 4bf92f3577b34da6a3ce929d0e0e4736 Bravo.Api.Problems.BravoExceptionHandler[1] The request GET /releases/latest ended with the error UpdateCheckFailed.
    Bravo.Core.Errors.BravoException: The update feed cannot be read.
```

The other lines of a message and the exception follow, each indented by four spaces.

**Who reads it.**

- Support, in the file: `%LOCALAPPDATA%\Bravo\Logs` for an installed or a portable copy. The trace of an error
  that the user reports leads to its record. In the Event Viewer, under Windows Logs, Application: the text of
  an entry begins with the category, the event ID, the trace and the scopes of the request, such as its path.
- Bravo sends the log nowhere. A company may collect the Event Log with its own tools.

**Never recorded.** The token of the API, passwords and credentials, the bodies and the headers of requests,
and the query of an address. A record may name a path in the profile of the user, such as the one of the
settings file.

## In the code

| Part | Path |
| --- | --- |
| The records that the pane reads, the types that travel, the route | `src/Bravo.Api/Diagnostics/` |
| The record of an error that a use case foresaw | `src/Bravo.Api/Problems/BravoExceptionHandler.cs` |
| The file of a run: its name, the runs kept, the limit, the writes | `src/Bravo.Platform.Windows/Diagnostics/LogFile.cs` |
| The log of the process: the levels, the record and its trace, the line of the file, the guard of the Event Log | `src/Bravo.Desktop/Diagnostics/` |
| The log opened after the arbitration, the records of the start and of the end | `src/Bravo.Desktop/Program.cs` |

- `Main` opens the log after the arbitration, so a second process writes nothing, and closes it after the
  host. `DesktopHost.Build` gets it as an instance ([ADR 0004](../decisions/0004-bootstrap-services-outside-the-container.md))
  and registers its factory and its buffer; `AddApi` adds a buffer of its own only when none is there, for the
  tests of the API.
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
- The tests open the log in a temporary data folder, without the Event Log. The one test that writes to the
  Event Log runs on a runner of GitHub Actions only, and finds its entry by a text of its own.
  `Log_SecretsThatRequestsCarry_AreNeverRecorded` sends a value in the place of the token, in the query and in
  the address of a proxy, and finds it in no record.
