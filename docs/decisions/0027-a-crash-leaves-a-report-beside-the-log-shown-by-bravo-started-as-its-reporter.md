# 0027. A crash leaves a report beside the log, shown by Bravo started as its reporter

- Status: Accepted
- Date: 2026-11-26

## Context

An exception that nothing caught ends the process with a message box, and Bravo records nothing. A failed
service of the host closes the window silently, with exit code 0. Support needs the exception, its trace and
the log of the run. A failing process may have a broken UI thread or webview.

## Decision

- **One rule.** An exception that nothing foresaw, on any thread or in the host, ends the process with a
  report. Recorded without a crash: an unobserved task exception, a defect in a request, an error of the page, a
  foreseen failure of the webview.
- **The failing process** writes the report and a critical record with the same trace. It starts
  `Bravo.exe --crash-report <file>` and returns; the runtime ends it with its Event Log entry and Windows
  Error Reporting. Other failing threads wait.
- **The report**: JSON in `Logs`, named after the log of the run, the 10 latest kept.
  - It holds the version, time, trace, log file, the exception with causes and stack.
  - Never: the names of user and machine, the command line, the environment, settings, credentials, memory.
- **The reporter mode** takes no instance, log or host, and sends the crash
  ([ADR 0028](0028-telemetry-as-in-1-0-x-on-the-classic-sdk.md)).
  - Its task dialog shows error, version, time, trace; Send; Report on GitHub opens a new issue; Copy details;
    Open folder; Close.
  - A crash in it, or before the data folder is known, gets a message box instead.
- **Exit codes** 1 to 5 and 1223 stay; a crash gets the code of the runtime.
- **Tests**: `--dev-crash=<place>` in development builds. Crash tests run on a runner only.

## Rejected alternatives

- The dialog in the failing process, as in 1.0.x and `main`: its modal loop pumps the broken window, and the
  Event Log entry waits for the user.
- A reporter of its own: one more executable to sign and install, for nothing a mode cannot do.
- Dumps or Windows Error Reporting settings: personal data, an administrator.

## Consequences

- The report and the log name each other, in one folder.
- A stack overflow, `FailFast`, a native crash or a hang leaves no report, and no end in the log of the run.
- This supersedes [ADR 0003](0003-fatal-exceptions-end-the-process.md).
