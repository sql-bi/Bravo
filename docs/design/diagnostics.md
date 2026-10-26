# Diagnostics

Exception reporting in the .NET host. The frontend has a separate
[telemetry client](../../src/Scripts/controllers/telemetry.ts); host flush and crash handling do not control it.

## Reporting paths

| Origin | Process outcome | Current reporting |
| --- | --- | --- |
| `AppDomain.UnhandledException`, including WinForms exceptions routed through `ThrowException` | Runtime terminates after the handler returns | Crash report saved and shown by a reporter process, which sends it only when the user clicks Send and consent allowed telemetry at the crash; after the bootstrap, also error log |
| `TaskScheduler.UnobservedTaskException` | Continues | Error log and telemetry subject to consent; no dialog or shutdown flush |
| Exception caught by an operation that can recover or return an error | Continues | Reporting belongs to the operation or its API boundary; see [handled exceptions](#handled-exceptions) |

The two runtime callbacks use
[GlobalExceptionHandling](../../src/Infrastructure/Diagnostics/GlobalExceptionHandling.cs). An unobserved task
exception is not a fatal crash, even though its telemetry has `ExceptionKind = Unhandled`. Why a fatal exception
ends the process: [ADR 0003](../adr/0003-fatal-exceptions-end-the-process.md).

## Startup coverage and fatal reporting

[Program](../../src/Program.cs) installs global handling before every other startup step, right after
`ApplicationConfiguration.Initialize`, which enables the visual styles of the reporter's task dialog, and the check
that runs the process as a crash reporter. Logging and telemetry do not exist yet: until `BootstrapContextFactory`
attaches them to the handler, a fatal exception is reported without a log entry from Bravo, and its report cannot be
sent, because consent is not known yet. The runtime still writes its own Event Log entry when it ends the process.
This covers single-instance
arbitration, directory and proxy setup, the WebView2 runtime check and bootstrap creation. A secondary instance exits
after the arbitration, so the whole process is covered this way
([single-instance contract](architecture.md#single-instance-contract)). `BootstrapContext` detaches the handler
before it disposes the services, so an exception raised during that disposal is reported the same way and never
through disposed services.
[BootstrapContextFactory](../../src/Host/BootstrapContextFactory.cs) degrades to default settings and disabled
telemetry when they cannot be created, but a policy read failure propagates on purpose, so that administrative
restrictions are never bypassed: a policy registry key that cannot be read ends the process through this path
([ADR 0005](../adr/0005-policy-failures-stop-startup.md)).

[UnhandledExceptionHandler](../../src/Infrastructure/Diagnostics/UnhandledExceptionHandler.cs) saves a fatal exception
as a crash report and starts the [reporter](../../src/Host/BravoCrashReporter.cs), then logs it: logging can fail for
the same reason the process crashed. The process that crashed sends nothing: a crash leaves the machine only when the
user clicks Send in the reporter, once, for a report saved while telemetry was allowed. If the report cannot be saved
or the reporter cannot start, a message box shows the error instead. An unobserved task exception is logged and
tracked; the two failures are isolated so one does not prevent the other.

The folder of the reports is computed without `AppEnvironment`, whose initialization can be the failure being
reported. The reporter is the same executable started with `--crash-report <file>`: it never claims the application
instance, so Bravo can start again while it is open, and it never reports its own crashes, which would otherwise
start another reporter. If its task dialog fails, a message box shows the error and the report path. It waits for a
send in progress, and for the telemetry to dispose, before it exits: exiting earlier loses the report.

Only the first fatal exception is handled, and the guard never opens again because the process ends after it. A
fatal exception on another thread blocks that thread until the process ends: returning would let the runtime end the
process while the first report is still being written or shown. A fatal exception raised on the thread that is
already handling one returns at once, and the runtime ends the process. Unobserved task reporting does not acquire
that guard, so it cannot suppress a fatal report.

If the global handler throws, `GlobalExceptionDispatcher` calls `Environment.FailFast`. Reporting failures must
therefore be contained where recovery is possible. Never show UI or wait for a shutdown flush on the unobserved-task
callback: it runs on the finalizer thread.

## Handled exceptions

For an operation with initialized logging and telemetry, report a caught failure through both `ILogger` and
`ITelemetryService.TrackException`: consent can disable the remote record, while the local log remains. Use the
reporting type's logger and add telemetry properties only for a specific diagnostic query. A failure that propagates
to the API boundary or to the global handler is reported there; reporting it again in a lower layer records it
twice. An expected condition that the code repairs, such as a token cache that
cannot be decrypted and is rebuilt at the next sign-in, is logged only: tracking it would count it among the failures.
These are rules for changes; existing catch sites do not all follow them yet.

### API boundary

[HostingExtensions.AddAndConfigureProblemDetails](../../src/Infrastructure/Extensions/HostingExtensions.cs) owns
reporting for exceptions mapped to API responses. Its mappings track exceptions. Specific mappings log the exception at
`Information`, which reaches the diagnostics pane only at diagnostic level `Verbose`, highlighted because it carries the
exception; the catch-all mapping does not log, because the problem details middleware already logs every response
with status 500 or higher as an error.
Exception details are enabled for the UI to display and copy. Do not add duplicate tracking in each controller for
exceptions that propagate to these mappings.

## Local logging and the diagnostics pane

There is no log file: users are asked to copy errors from the diagnostics pane, and IT support reads the Event Log.
The host logger factory, created by [BootstrapContextFactory](../../src/Host/BootstrapContextFactory.cs), writes
`Warning` and above of every category, ASP.NET Core and Kestrel included, to the Windows Event Log and to the
[DiagnosticLogStore](../../src/Infrastructure/Diagnostics/DiagnosticLogStore.cs) read by the pane. At diagnostic level
`Verbose` the store also receives `Information` records, except from the `Microsoft` and `System` categories, whose
records below `Warning` describe every request, the pane's own polling included.
[DiagnosticLevelSwitch](../../src/Infrastructure/Diagnostics/DiagnosticLevelSwitch.cs) carries the level: the
logger factory exists before the user settings are read, so the bootstrap applies the saved level and `UpdateOptions`
applies a changed one. Code that writes to the pane logs through `ILogger`, and guards an expensive argument, such as
a serialized payload, with `IsEnabled(LogLevel.Information)`. Never log tokens or connection strings with passwords:
the pane shows them, and redaction removes only the text inside sensitive tags.

| Store property | Consequence |
| --- | --- |
| Created with the logger factory and registered in the container as an instance | Records written before the host exists, during the bootstrap, reach the pane |
| Holds the last 1000 records and at most 8M characters of text, oldest evicted first | Memory is bounded even when records carry large payloads; a read reports how many records after its cursor it does not return |
| Each record has a sequence number; reading does not change the store | The pane keeps the cursor, so a reload reads every record held again and two readers do not take records from each other |
| A read takes a maximum count and returns the newest records | The pane asks only for as many records as it keeps |
| A record keeps the template, the placeholder values and the exception chain apart from the formatted message | The pane shows a JSON value as a tree; the formatted message is the plain text of the record |
| The exception chain holds every inner exception, those of an aggregate included, and the public properties each exception type adds, such as an error or status code | What a type writes in its `ToString` override is kept without the unstructured text; a property that exposes a secret reaches the pane, so such a type must not be logged as it is |

The [frontend logger](../../src/Scripts/controllers/logger.ts) polls `GetDiagnostics` at every diagnostic level except
`None`. At `Basic` it shows host records of level `Warning` and above; `Verbose` adds the `Information` records.
A record of level `Warning` and above, or one that carries an exception, is highlighted.
The app's own messages, those without the `[HOST]` prefix, live only in the page, so a reload loses them. Clear
empties the view only: after a reload the pane reads the host records again from the store.

The store keeps the original text and the pane shows it, because the user who reads it owns the data and needs the
names of the server, database or account that failed. Redaction happens where the text leaves the machine:

| Way out | Redaction |
| --- | --- |
| A copied message; Report issue | `Utils.Obj.anonymize`: the same five tags as [SensitiveTags](../../src/Infrastructure/Telemetry/SensitiveTags.cs), plus the values of personal keys such as `email` and `serverName` |
| Host telemetry | `DefaultTelemetryProcessor`, in exception messages, through [SensitiveTextRedactor](../../src/Infrastructure/Telemetry/SensitiveTextRedactor.cs): `SensitiveTags`, then the user profile folders |
| Crash report: saved file, the dialog's Copy and Send | `SensitiveTextRedactor`, in the message and in the whole exception text, when the report is created |
| Event Log | None: it stays on the machine and IT support reads it |

The two tag lists, in C# and in TypeScript, change together; `SensitiveTagsTests` fails when they differ.
`Utils.Obj.anonymize` removes only tagged text and personal keys, so copied text can still include report, server
and database names, and the user profile folders.

## Telemetry consent

The host uses Application Insights through [TelemetryService](../../src/Infrastructure/Telemetry/TelemetryService.cs).
The `TelemetryEnabled` setter assigns the value it receives, so every caller resolves the consent first:

| Entry point | Current behavior |
| --- | --- |
| Bootstrap | [TelemetryConsent.Resolve](../../src/Infrastructure/Telemetry/TelemetryConsent.cs) gives a configured policy precedence over the user preference |
| Preferences saved through `UpdateOptions` | [ApplicationController](../../src/Controllers/ApplicationController.cs) resolves the consent the same way before assigning it |
| Crash report | The report records `TelemetryEnabled` at the crash: false before the bootstrap and with `NullTelemetryService`. The reporter creates its own enabled service only to send a report that recorded true |

A disabled service drops new tracked exceptions.

Never add personal data to telemetry messages or properties. The single-instance pipe name includes the user SID and
must not be recorded. [DefaultTelemetryProcessor](../../src/Infrastructure/Telemetry/DefaultTelemetryProcessor.cs)
redacts only the tagged text and the user profile folders of exception messages: it is not a general sanitizer.

### Flush and shutdown

Why the channel persists telemetry: [ADR 0006](../adr/0006-persistent-telemetry-channel.md).
`TelemetryService.TrackException` for an exception calls the synchronous `Flush`, which moves telemetry into the
channel's transmission pipeline and returns without waiting for delivery. The overload for a prepared
`ExceptionTelemetry`, which the reporter uses, does not: the reporter awaits `FlushAsync`, which then carries the
report, and a `Flush` before it would block the dialog. In the
[ServerTelemetryChannel 2.23.0 contract](https://raw.githubusercontent.com/microsoft/ApplicationInsights-dotnet/2.23.0/BASE/src/ServerTelemetryChannel/ServerTelemetryChannel.cs),
`FlushAsync` succeeds once the data has left the process, sent or persisted locally; success does not prove
ingestion, so the dialog says the report has been sent or will be sent at the next start. Every path that ends the
process or confirms a report awaits `FlushAsync` with a bounded timeout: `TelemetryService.Dispose` and the
reporter's Send. Do not replace that wait with the synchronous `Flush`.

`TelemetryService.Dispose` blocks the caller for at most 5 seconds; a flush failure other than cancellation
propagates.

The channel stores unsent transmissions under `.cache\.telemetry` in the application data folder when it can persist
them; data still in memory at termination is lost. The
[telemetry tests](../../test/Bravo.Tests/Infrastructure/Telemetry/TelemetryServiceTests.cs) cover delivery at
shutdown and persistence when the endpoint is unavailable.
