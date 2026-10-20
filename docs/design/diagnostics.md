# Diagnostics

Exception reporting in the .NET host. The frontend has a separate
[telemetry client](../../src/Scripts/controllers/telemetry.ts); host flush and crash handling do not control it.

## Reporting paths

| Origin | Process outcome | Current reporting |
| --- | --- | --- |
| `AppDomain.UnhandledException`, including WinForms exceptions routed through `ThrowException` | Runtime terminates after the handler returns | Saved report and crash dialog; after the bootstrap, also error log and telemetry subject to consent |
| `TaskScheduler.UnobservedTaskException` | Continues | Error log and telemetry subject to consent; no dialog or shutdown flush |
| Exception caught by an operation that can recover or return an error | Continues | Reporting belongs to the operation or its API boundary; see [handled exceptions](#handled-exceptions) |

The two runtime callbacks use
[GlobalExceptionHandling](../../src/Infrastructure/Diagnostics/GlobalExceptionHandling.cs). An unobserved task
exception is not a fatal crash, even though its telemetry has `ExceptionKind = Unhandled`. Why a fatal exception
ends the process: [ADR 0003](../adr/0003-fatal-exceptions-end-the-process.md).

## Startup coverage and fatal reporting

[Program](../../src/Program.cs) installs global handling right after `ApplicationConfiguration.Initialize`, which
enables the visual styles the task dialog needs, and before every other startup step. Logging and telemetry do not
exist yet: until `BootstrapContextFactory` attaches them to the handler, a fatal exception is
saved to the report and shown in the dialog, without a log entry from Bravo, without telemetry and without the Send
button. The runtime still writes its own Event Log entry when it ends the process. This covers single-instance
arbitration, directory and proxy setup, the WebView2 runtime check and bootstrap creation. A secondary instance exits
after the arbitration, so the whole process is covered this way
([single-instance contract](architecture.md#single-instance-contract)). `BootstrapContext` detaches the handler
before it disposes the services, so an exception raised during that disposal is reported the same way and never
through disposed services.
[BootstrapContextFactory](../../src/Host/BootstrapContextFactory.cs) degrades to default settings and disabled
telemetry when they cannot be created, but a policy read failure propagates on purpose, so that administrative
restrictions are never bypassed: a policy registry key that cannot be read ends the process through this path
([ADR 0005](../adr/0005-policy-failures-stop-startup.md)).

[UnhandledExceptionHandler](../../src/Infrastructure/Diagnostics/UnhandledExceptionHandler.cs) logs and tracks each
callback before attempting a fatal report. Logging and tracking failures are isolated so one does not prevent the
other. For a fatal exception, a marker in `Exception.Data` suppresses a repeated dialog and report; it does not
suppress the earlier log and telemetry calls.

[ErrorReport](../../src/Infrastructure/Diagnostics/ErrorReport.cs) saves environment diagnostics and the exception to
`ErrorReport.txt` under the application data folder; the folder is resolved only when a report is saved, because
`AppEnvironment` initialization can be the failure being reported. A failed save does not prevent the dialog. If the
task dialog fails, for example because a type it reads failed to initialize, a plain message box shows the error and
the report path instead. The dialog failure is logged once the bootstrap services are attached, and it never prevents
the shutdown flush.
[ErrorReportTaskDialog](../../src/Infrastructure/Diagnostics/ErrorReportTaskDialog.cs) runs on a dedicated STA thread
and offers copying, closing and, when host telemetry is disabled, sending the exception through telemetry. Automatic
tracking is an attempt, not confirmation of delivery.

The fatal-handler guard suppresses another fatal arrival while a fatal handler runs. It does not coordinate
simultaneous crashes or guarantee that the first dialog remains open until the user responds. Unobserved task
reporting does not acquire that guard, so it cannot suppress a fatal report.

If the global handler throws, `GlobalExceptionHandling` calls `Environment.FailFast`. Reporting failures must
therefore be contained where recovery is possible. Never show UI or wait for a shutdown flush on the unobserved-task
callback: it runs on the finalizer thread.

## Handled exceptions

For an operation with initialized logging and telemetry, report a caught failure through both `ILogger` and
`ITelemetryService.TrackException`: consent can disable the remote record, while the local log remains. Use the
reporting type's logger and add telemetry properties only for a specific diagnostic query. A failure that propagates
to the API boundary or to the global handler is reported there; reporting it again in a lower layer records it
twice. These are rules for changes; existing catch sites do not all follow them yet.

### API boundary

[HostingExtensions.AddAndConfigureProblemDetails](../../src/Infrastructure/Extensions/HostingExtensions.cs) owns
reporting for exceptions mapped to API responses. Its mappings track exceptions. Specific mappings add host
diagnostics at `Verbose`; the catch-all mapping adds diagnostics regardless of that setting. Exception details are
enabled for the UI to display and copy. Do not add duplicate tracking in each controller for exceptions that
propagate to these mappings.

## Local logging and the diagnostics pane

The host logger factory, created by [BootstrapContextFactory](../../src/Host/BootstrapContextFactory.cs), writes
`Warning` and above of every category, ASP.NET Core and Kestrel included, to the Windows Event Log and to
[AppEnvironment.Diagnostics](../../src/Infrastructure/AppEnvironment.cs). That store also receives explicit
`AddDiagnostics` calls, lives in memory for the whole process and has no eviction. The
[frontend logger](../../src/Scripts/controllers/logger.ts) polls `GetDiagnostics` only at diagnostic level
`Verbose`: a message can be stored without ever appearing in the pane.

## Telemetry consent

The host uses Application Insights through [TelemetryService](../../src/Infrastructure/Telemetry/TelemetryService.cs).
The `TelemetryEnabled` setter assigns the value it receives, so every caller resolves the consent first:

| Entry point | Current behavior |
| --- | --- |
| Bootstrap | [TelemetryConsent.Resolve](../../src/Infrastructure/Telemetry/TelemetryConsent.cs) gives a configured policy precedence over the user preference |
| Preferences saved through `UpdateOptions` | [ApplicationController](../../src/Controllers/ApplicationController.cs) resolves the consent the same way before assigning it |
| Crash dialog Send | Temporarily enables host telemetry, tracks the exception and awaits one flush, then restores the previous value |

A disabled service drops new tracked exceptions. If bootstrap supplied `NullTelemetryService`, enabling it or
requesting a flush still cannot send a report, so the crash handler treats it as no telemetry and the dialog does not
offer Send.

Never add personal data to telemetry messages or properties. The single-instance pipe name includes the user SID and
must not be recorded. [DefaultTelemetryProcessor](../../src/Infrastructure/Telemetry/DefaultTelemetryProcessor.cs)
redacts supported sensitive tags in exception messages; it is not a general sanitizer for arbitrary properties or
untagged data.

### Flush and shutdown

Why the channel persists telemetry: [ADR 0006](../adr/0006-persistent-telemetry-channel.md).
`TelemetryService.TrackException` calls the synchronous `Flush`, which moves telemetry into the channel's
transmission pipeline and returns without waiting for delivery. In the
[ServerTelemetryChannel 2.23.0 contract](https://raw.githubusercontent.com/microsoft/ApplicationInsights-dotnet/2.23.0/BASE/src/ServerTelemetryChannel/ServerTelemetryChannel.cs),
`FlushAsync` succeeds once the data has left the process, sent or persisted locally; success does not prove
ingestion. Every path that ends the process or confirms a report awaits `FlushAsync` with a bounded timeout:
`TelemetryService.Dispose`, the fatal handler after the dialog closes, and the crash dialog's Send. Do not replace
that wait with the synchronous `Flush`.

`TryFlushBeforeShutdown` blocks the caller and returns `false` only on cancellation; other exceptions propagate.

The channel stores unsent transmissions under `.cache\.telemetry` in the application data folder when it can persist
them; data still in memory at termination is lost. The
[telemetry tests](../../test/Bravo.Tests/Infrastructure/Telemetry/TelemetryServiceTests.cs) cover delivery at
shutdown and persistence when the endpoint is unavailable.
