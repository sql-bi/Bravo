# Diagnostics

How Bravo records and reports exceptions. Two paths, chosen by whether the process survives.

| Path | Trigger | Process | Destination |
| --- | --- | --- | --- |
| Unhandled | an exception nobody caught | terminates | crash dialog; report file; Event Log; telemetry if the user sends it |
| Handled | an exception caught by code that can continue | continues | `ILogger` (Event Log, diagnostics pane); telemetry |

An exception belongs to exactly one path. Code that catches an exception and continues must log it and track
it; an exception that is caught and neither logged nor tracked is invisible, and the failure it reports
recurs with no record.

## Unhandled path

Configured once at startup, before the host is built, by `BravoGlobalExceptionHandling` using the bootstrap
services (logger factory, telemetry). It does not depend on the DI container, so it covers failures while the
container is being built.

Hooks:

| Hook | Origin | Constraint |
| --- | --- | --- |
| `AppDomain.UnhandledException` | `AppDomain` | the runtime terminates the process when the handler returns |
| `TaskScheduler.UnobservedTaskException` | `UnobservedTask` | runs on the finalizer thread: no UI, no blocking; the process continues |
| WinForms `UnhandledExceptionMode.ThrowException`, all threads | routes UI-thread exceptions to `AppDomain` | keeps the stack intact at the throw site; WinForms' own Continue/Quit dialog is never shown |

Handler steps, in order:

1. Log at `Error` with the origin, then track in telemetry with `ExceptionOrigin` and `ExceptionKind = Unhandled`.
   Each step ignores its own failures so that the next one runs.
2. `UnobservedTask`: stop here.
3. Mark the exception as handled through `Exception.Data`; a second arrival of the same exception stops here.
4. Build an `ErrorReport` (environment diagnostics plus the exception) and save it as `ErrorReport.txt` under
   the application data folder. A failed save does not stop the dialog.
5. Show the crash dialog on an STA thread. Buttons: **Send report** (visible only when telemetry is off, because
   step 1 already sent it otherwise), **Copy to clipboard**, **Close**. Send enables telemetry for the duration
   of one flush, with a timeout, then restores the previous consent.
6. Flush telemetry with a timeout, then return; the runtime terminates the process.

A handler that throws ends the process with `Environment.FailFast`, carrying both exceptions.

Unobserved task reporting does not acquire the fatal-handler guard: a non-fatal report must not suppress a
fatal exception. The guard only suppresses fatal arrivals while another fatal handler runs; simultaneous
fatal crashes are not coordinated.

## Handled path

Code that catches an exception and continues reports it twice, on purpose: the logger is local and immediate,
telemetry is remote and subject to consent.

- `ILogger`, category of the reporting type, level by outcome: `Warning` when the application continues
  unaffected, `Error` when an operation the user asked for failed. Structured events follow
  [code-conventions.md](code-conventions.md#logging).
- `ITelemetryService.TrackException(exception)`, without properties. The exception type and stack trace
  identify the failure; a property is added only for a query that exists.

Logger sinks, configured in `BootstrapContextFactory`:

| Sink | Minimum level | Purpose |
| --- | --- | --- |
| EventSource | all | ETW tracing |
| Windows Event Log, `Application` | `Warning` | record on the machine, survives the process |
| Diagnostics pane | `Warning` | visible to the user inside Bravo |
| Console, Debug | all, Debug builds only | development |

API exceptions follow the same path through the ProblemDetails mappings in `HostingExtensions`: each mapping
tracks the exception and, at verbose diagnostic level, adds it to the diagnostics pane; the response carries
the exception details so the UI can show and copy them.

## Diagnostics pane

An in-memory list of messages (`AppEnvironment.Diagnostics`), read by the UI through the `GetDiagnostics`
API. It receives every log entry at `Warning` or above through the logger, and explicit entries from code that
calls `AppEnvironment.AddDiagnostics`. The list is per process: it is lost at exit, which is why the Event Log
sink exists.

## Telemetry

Application Insights. Consent is resolved by `TelemetryConsent` at startup and again when the user saves the
preferences: a configured policy overrides the user setting. When consent is off, `TrackException` drops the
exception; the crash dialog's Send button is the only override, scoped to one flush.

Never put personal data in a message or a property. The pipe name of the single-instance protocol contains
the user SID and stays out of telemetry.

### Flush at exit

The channel sends its buffer on a timer and holds anything tracked since the last send. Only `FlushAsync`
waits for the transmission: the synchronous `Flush` hands the buffer to the sender and returns before the
request is sent, and a process that exits right after loses it without a trace, since the channel persists a
transmission only when it fails.

| Exit | Flush | Bound |
| --- | --- | --- |
| Normal close | `TelemetryService.Dispose`, `TryFlush` | 5 s |
| Crash | `UnhandledExceptionHandler`, `TryFlush` | 10 s |
| Crash dialog Send | `FlushAsync` | 30 s, outcome shown to the user |

`TryFlush` is the synchronous, bounded, non-throwing form for callers that run right before exit; an
exception there would end the crash path with `FailFast` and a different recorded cause. `FlushAsync` is for
callers that can await and act on the outcome.

The bound exists for an unreachable endpoint, which fails only after the TCP timeout. A transmission that
fails within the bound is retried at the next start if the channel can store it on disk, otherwise it is
dropped; one still pending at the bound is lost with the process.

## Rules

| Rule | Violated when | Consequence |
| --- | --- | --- |
| Catch only what the code can handle | a `catch` swallows and continues | the failure recurs with no record |
| Every handled exception is logged and tracked | one of the two calls is missing | no local trace, or no field data |
| The unhandled handler never throws | a step is not guarded | `FailFast`, no dialog, no report file |
| No UI on the finalizer thread | an `UnobservedTask` reaches the dialog | the finalizer thread blocks |
| Level by outcome, not by exception type | a timeout the app survives is logged as `Error` | Event Log noise hides real errors |
