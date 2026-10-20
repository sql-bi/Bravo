# 0003. Fatal exceptions end the process after a report

- Status: Accepted
- Date: 2026-10-20

## Context

Bravo is a thin WinForms shell around a WebView2 UI and an in-process API. An exception that escapes a thread,
the UI thread included, leaves that shell in an unknown state. The users are business analysts, who need to know
that the application failed and need something they can pass to support.

## Decision

- `GlobalExceptionHandling` sets `UnhandledExceptionMode.ThrowException` for every thread and handles
  `AppDomain.UnhandledException` and `TaskScheduler.UnobservedTaskException`.
- A fatal exception is logged, tracked through telemetry when consent allows it, saved to `ErrorReport.txt` in the
  application data folder and shown in a task dialog. The runtime then ends the process.
- When telemetry is disabled, the dialog offers to send that one report; nothing is sent without that action.
- An unobserved task exception is logged and tracked, without a dialog, and the process continues.
- If the handler itself fails, `Environment.FailFast` ends the process.

## Rejected alternatives

- Continue after a UI-thread exception (`CatchException` with `Application.ThreadException`): the window stays
  open in a broken state, and exceptions on other threads are not covered.
- Keep the process alive at any cost, parking faulting threads: the API and the WebView2 bridge cannot be trusted
  after an unknown failure.
- Report through Windows Error Reporting: it adds native interop and an approval flow, for a report that a text
  file and telemetry already carry.
- Track fatal exceptions only after the user confirms: crashes would be lost whenever the user closes the dialog,
  although they consented to telemetry.

## Consequences

- No work survives a fatal exception; a crash is visible, never silent.
- The report file is overwritten by the next crash: it serves the dialog that shows it, not as a history.
- Exceptions raised before the handler is installed are not covered; see
  [diagnostics](../design/diagnostics.md#startup-coverage-and-fatal-reporting).
- Code on the finalizer thread, where unobserved task exceptions arrive, must never show UI or wait.
