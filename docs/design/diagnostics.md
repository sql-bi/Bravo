# Diagnostics

Exception reporting in the .NET host. The frontend has a separate [telemetry client](../../src/Scripts/controllers/telemetry.ts); host flush and crash handling do not control that client.

## Reporting paths

| Origin | Process outcome | Current reporting |
| --- | --- | --- |
| `AppDomain.UnhandledException`, including WinForms exceptions routed through `ThrowException` | Runtime terminates after the handler returns | Error log; telemetry subject to consent; saved report and crash dialog |
| `TaskScheduler.UnobservedTaskException` | Continues | Error log and telemetry subject to consent; no dialog or shutdown flush |
| Exception caught by an operation that can recover or return an error | Continues | Reporting belongs to the operation or its API boundary; apply the [handled-exception rules](#handled-exception-rules) |

The two runtime callbacks use [GlobalExceptionHandling](../../src/Infrastructure/Diagnostics/GlobalExceptionHandling.cs). An unobserved task exception is not a fatal crash, even though its telemetry has `ExceptionKind = Unhandled`.

## Startup coverage and fatal reporting

[Program](../../src/Program.cs) installs global handling after application configuration and bootstrap creation, before building the host. Exceptions during directory setup, proxy configuration or bootstrap creation occur before these hooks exist. Host construction is covered; earlier initialization is not.

[UnhandledExceptionHandler](../../src/Infrastructure/Diagnostics/UnhandledExceptionHandler.cs) logs and tracks each callback before attempting a fatal report. Logging and tracking failures are isolated so one does not prevent the other. For a fatal exception, a marker in `Exception.Data` suppresses a repeated dialog and report; it does not suppress the earlier log and telemetry calls.

[ErrorReport](../../src/Infrastructure/Diagnostics/ErrorReport.cs) saves environment diagnostics and the exception to `ErrorReport.txt` under the application data folder. A failed save does not prevent the dialog. [ErrorReportTaskDialog](../../src/Infrastructure/Diagnostics/ErrorReportTaskDialog.cs) runs on a dedicated STA thread and offers copying, closing and, when host telemetry is disabled, sending the exception through telemetry. Automatic tracking is an attempt, not confirmation of delivery.

The fatal-handler guard suppresses another fatal arrival while a fatal handler runs. It does not coordinate simultaneous crashes or guarantee that the first dialog remains open until the user responds. Unobserved task reporting does not acquire that guard, so it cannot suppress a fatal report.

If the global handler throws, `GlobalExceptionHandling` calls `Environment.FailFast`. Reporting failures must therefore be contained where recovery is possible. Never show UI or wait for a shutdown flush on the unobserved-task callback: it runs on the finalizer thread.

## Handled-exception rules

For an operation with initialized logging and telemetry services, report a caught failure through both `ILogger` and `ITelemetryService.TrackException`. Local logging and remote telemetry serve different purposes; consent can disable the remote record. Use the reporting type's logger and the [logging conventions](code-conventions.md#logging). Add telemetry properties only when needed by a specific diagnostic query.

These rules have explicit boundaries:

| Condition | Reporting responsibility | Reason |
| --- | --- | --- |
| A failure propagates to an API or global handler | Leave reporting to that boundary unless this layer handles a separate failure | Repeated reporting records the same failure more than once |
| Settings or telemetry creation fails during bootstrap | Use the available bootstrap logger; do not depend on telemetry creation succeeding | Reporting must not require the service whose creation failed |
| Logging, tracking or another recovery step itself fails | Use an independent fallback when available; do not recursively call the failing sink | Recursion can replace the original failure or end recovery |
| An exception represents an explicitly supported outcome, such as cancellation or an unavailable optional capability | Follow that operation's contract; document an intentional ignored exception at the catch site | Expected outcomes are not automatically application failures |

These are requirements for changes, not a claim that every existing catch site already uses both services. [BootstrapContextFactory](../../src/Host/BootstrapContextFactory.cs), for example, logs settings and telemetry creation failures and continues with defaults or disabled telemetry; policy initialization failures propagate.

### API boundary

[HostingExtensions.AddAndConfigureProblemDetails](../../src/Infrastructure/Extensions/HostingExtensions.cs) owns reporting for exceptions mapped to API responses. Its mappings track exceptions. Specific mappings add host diagnostics at `Verbose`; the catch-all mapping adds diagnostics regardless of that setting. Exception details are enabled for the UI to display and copy. Do not add duplicate tracking in each controller for exceptions that propagate to these mappings.

## Local logging and the diagnostics pane

[BootstrapContextFactory](../../src/Host/BootstrapContextFactory.cs) configures the host logger factory:

| Sink | Filter configured here | Destination |
| --- | --- | --- |
| EventSource | No provider-specific minimum | ETW listeners, subject to effective logging filters |
| Windows Event Log | `Warning` and above | `Application` log |
| DiagnosticMessageLoggerProvider | `Warning` and above | In-memory host diagnostics |
| Console and Debug | No provider-specific minimum; Debug builds only | Development output, subject to effective logging filters |

[AppEnvironment.Diagnostics](../../src/Infrastructure/AppEnvironment.cs) stores messages written by the diagnostics logger and explicit `AddDiagnostics` calls. Collection in this store is separate from visibility in the UI. The [frontend logger](../../src/Scripts/controllers/logger.ts) polls `GetDiagnostics` only at diagnostic level `Verbose`; host messages may be collected without appearing in the pane. The host store is lost at process exit.

## Telemetry consent

The host uses Application Insights through [TelemetryService](../../src/Infrastructure/Telemetry/TelemetryService.cs). Consent is applied differently at each entry point:

| Entry point | Current behavior |
| --- | --- |
| Bootstrap | [TelemetryConsent.Resolve](../../src/Infrastructure/Telemetry/TelemetryConsent.cs) gives a configured policy precedence over the user preference |
| Preferences saved through `UpdateOptions` | [ApplicationController](../../src/Controllers/ApplicationController.cs) assigns the raw user preference; the service setter does not reapply policy precedence |
| Crash dialog Send | Temporarily enables host telemetry, tracks the exception and awaits one flush, then restores the previous value |

Do not assume startup policy precedence also holds after saving preferences. A disabled service drops new tracked exceptions. If bootstrap supplied `NullTelemetryService`, enabling it or requesting a flush still cannot send a report.

Never add personal data to telemetry messages or properties. The single-instance pipe name includes the user SID and must not be recorded. [DefaultTelemetryProcessor](../../src/Infrastructure/Telemetry/DefaultTelemetryProcessor.cs) redacts supported sensitive tags in exception messages; it is not a general sanitizer for arbitrary properties or untagged data.

### Flush and shutdown

`TelemetryService.TrackException` calls synchronous `Flush` to move buffered telemetry into the channel's transmission pipeline. That call does not wait for network delivery. The [ServerTelemetryChannel 2.23.0 contract](https://raw.githubusercontent.com/microsoft/ApplicationInsights-dotnet/2.23.0/BASE/src/ServerTelemetryChannel/ServerTelemetryChannel.cs) defines `FlushAsync` success as transfer out of process, which can mean server delivery or local persistence; success is not proof that ingestion has completed.

| Caller | Operation | Cancellation timeout |
| --- | --- | --- |
| `TelemetryService.Dispose` | `TryFlushBeforeShutdown` | 5 s |
| Fatal `UnhandledExceptionHandler`, after the dialog closes | `TryFlushBeforeShutdown` | 10 s |
| Crash dialog Send | `FlushAsync` | 30 s |

`TryFlushBeforeShutdown` blocks the caller and returns `false` on `OperationCanceledException`. It does not catch other exception types. Its name must not be treated as a general no-throw contract.

The server channel stores unsent transmissions under `.cache/.telemetry` in the application data folder when it can persist them. Data still only in memory at termination can be lost. Do not remove the awaited shutdown flush on the assumption that synchronous `Flush` guarantees delivery. The [telemetry tests](../../test/Bravo.Tests/Infrastructure/Telemetry/TelemetryServiceTests.cs) cover shutdown delivery to a local endpoint and persistence when the endpoint is unavailable.
