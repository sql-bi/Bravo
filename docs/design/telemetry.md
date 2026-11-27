# Telemetry

Bravo sends the telemetry of 1.0.x: to the same resource of Application Insights, with the same names and
values, and with the same consent
([ADR 0028](../decisions/0028-telemetry-as-in-1-0-x-on-the-classic-sdk.md)).

**Who sends what.** The host sends the exceptions that nothing foresaw:

- an exception of a request of the API that no use case foresaw, which the API answers with 500 ([API](api.md));
- a failure of the pipe through which a second start hands its request over ([Startup](startup.md));
- the exception of a task that failed and that nothing awaited ([Diagnostics](diagnostics.md));
- a failure of the engine of the webview that ends the application ([Window and webview](window-and-webview.md)).

An error that a use case foresaw, answered with 409, is not sent: the answer says what happened. Nor is a
runtime of the webview that is not installed, which the user is told to install. An exception after the answer
of a request has begun, which no route can raise today, is in the log alone ([API](api.md)).

The page sends what it tracks, with the names of 1.0.x: events, such as `Start`, `End`, `Theme` and `Link`; the
pages that it shows, such as `Welcome`; the seconds spent on a page, the metric `PageVisitTime`; the errors that
it reports itself, with their status, their title and their trace. It sends them itself, with the SDK of
Application Insights for JavaScript, at most 15 seconds after it tracked them. As in 1.0.x, the SDK also sends
its own critical errors as traces, every 10 seconds: among them the user agent of the webview, at the start and
after each page, since without a cookie the SDK takes the user for a new one each time.

The reporter of a crash sends the exception of its report (below). The custom action library that the
packages run, `Bravo.Setup.CA.dll`, sends the two events of 1.0.x, `Install` and `Uninstall`
([Installer](installer.md)).

**Consent.** A policy `TelemetryEnabled` of an administrator decides, on or off, whatever the user chose
([Policies](policies.md)). Without one, the option of the user decides: on by default, or the choice of the
installation for a user who has no settings yet ([Settings](settings.md)).

- The host and the page follow the option when the user changes it. Turned off, they stop at once: nothing
  more is sent, not even what waits to be sent, which they drop. Turned on, they send from then, and the host
  what waits in its folder. The page tracks its start once in a run: when telemetry is on at its start, or
  when the user turns it on.
- The log records the state when the host starts and at each change: `Telemetry is on: Policy.` or `Option.`,
  `Telemetry is off: Policy.` or `Option.`, or `Telemetry is off: this build sends it nowhere.`
- The events of the installer follow the choice of who installs alone ([Installer](installer.md)).

**The crash.** The process that crashes sends nothing. The reporter, started to show its report
([Diagnostics](diagnostics.md)), sends the exception of the report as an item of the run that crashed: its
session, its trace as the operation, its time, and the property `ExceptionKind`, `Unhandled`. It reads the
policies and the settings of the process that crashed, as the host does, and writes neither:

| When | The reporter |
| --- | --- |
| The build sends nowhere, or an earlier build wrote the report | Sends nothing |
| The policies cannot be read, or a policy turns telemetry off | Sends nothing |
| A policy turns telemetry on, or the option of the user does | Sends at once, through the folder of the host: what it could not send within 5 seconds waits there for the next start |
| The option of the user is off, or the settings cannot be read | Sends only when the user clicks Send, which works once, through the folder of the host: what it could not send waits there, and the next start with telemetry off deletes it. The dialog says that sending failed |

The dialog says what became of the crash ([Diagnostics](diagnostics.md)).

**What an item carries.** The names and the values of 1.0.x: the queries of the resource read the items of every
version alike.

| Field | Value |
| --- | --- |
| `ai.user.id` | The SHA-256, in lowercase hexadecimal, of `<computer>\<user>`: the same for one user on one computer in every version |
| `ai.session.id` | A new identifier for each run |
| `ai.cloud.roleInstance`, `ai.internal.nodeName` | The SHA-256 of the name of the computer, in both: the SDK would send the name in the one left empty |
| `ai.device.osVersion` | The version of Windows, as .NET writes it |
| `ai.application.ver` | The semantic version of the build |
| `ai.operation.id` | The trace of the work that failed, which its records in the log also carry ([Diagnostics](diagnostics.md)) |
| Properties | `ProductName` (`Bravo`), `Version`, `Build` (the file version), `PublishMode` (`SelfContained`, `FrameworkDependent`, or `None` for a development build), `InstallScope` (`PerMachine`, `PerUser`, or `Portable` for a copy that no package installed), `WebView2Version` |
| Exceptions | For each exception of the chain: its type, its message, and the frames of its stack with the method, the assembly, the source file and the line |

The items of the page carry the same user, run, version and properties, which the host gives it, and no
version of Windows and no role instance. The SDK of the page adds the kind of device, `Browser`, the path of the
page as the name of the operation, an operation identifier of its own, and its version.

An event of the installer carries the user, the version of Windows and the version of the package, a session
of its own, and the properties `InstallScope` (`perMachine` or `perUser`), `ProductName`, `Version` and
`Build`, as in 1.0.x. It carries no role instance and no node name: nothing names the computer.

**What never leaves the computer.**

- The text that Analysis Services and other libraries mark with `<pii>`, `<oii>`, `<euii>`, `<eupi>` or
  `<conn>` becomes `[REDACTED]`. A text too costly to scan, beyond one second, is withheld whole.
- In a message and in the path of a source file, the folder of the profile, the name of the user and the names
  of the computer become `%USERPROFILE%`, `%USERNAME%` and `%COMPUTERNAME%`.
- The name of the computer, which only its hash stands for.
- What the SDK of the page would track by itself: the exceptions of the page, its requests, the changes of its
  address, the timing of a page view.

**Limits.** As the SDK: the first 10 exceptions of a chain, inner and aggregated, depth first, and 32,768
characters of frames for each, keeping the frames of both ends. An aggregate of one exception is sent as that
exception, as in 1.0.x. A run sends every exception, with no limit of its own.

**Where it goes.** A release build sends to the resource of 1.0.x. A development build sends nowhere, unless it
is started with `--dev-telemetry-endpoint=<origin>`, an origin such as `http://127.0.0.1:8097`: it then sends
to that endpoint, under an instrumentation key of no resource, all zeros ([window and
webview](window-and-webview.md)). The program of the packages sends to the same resource; any other build of it
sends nowhere, and has no option for an endpoint ([Installer](installer.md)).

`GET /telemetry` gives the page the same destination and the values of the host: the user, the run, the
version and the properties ([API](api.md)). For a build that sends nowhere the answer has no connection string.
No endpoint of the connection string ends with a slash: the SDK of the page adds its path to the endpoint as it
is written.

**When it cannot send.** What the SDK could not send waits in the folder `Telemetry` of the data folder. The
stop of the host waits for it to be sent or kept, at most 3 seconds within the stop; so does the disposal of a
host whose start failed, which is not stopped. The next start that has the consent sends it. The folder holds
at most 50 MB, the default of the SDK (`ServerTelemetryChannel.MaxTransmissionStorageCapacity`): beyond it,
what the SDK could not send is lost. The SDK also drops a file that has waited for 2 days
([`TransmissionStorage`](https://github.com/microsoft/ApplicationInsights-dotnet/blob/2.23.0/BASE/src/ServerTelemetryChannel/Implementation/TransmissionStorage.cs)),
which the resource would refuse: it accepts no item older than 48 hours
([missing telemetry](https://learn.microsoft.com/troubleshoot/azure/azure-monitor/app-insights/telemetry/investigate-missing-telemetry)).
The SDK sends through the default proxy of the process, which the host and the reporter set from the settings,
with the credentials of the Credential Manager ([Network](network.md)); a proxy that refuses them loses that
batch. The page sends through the proxy that the host gives the webview, which answers with the same credentials
only the proxy server of the endpoint ([Network](network.md)). The page keeps what waits, and what it posted
without an answer, in the session storage of the webview, and sends it when the page loads again. The installer
sends each event once, through the proxy of the system with the Windows credentials of who installs, and keeps
nothing: an installation waits for it 5 seconds at most.

## In the code

| Part | Path |
| --- | --- |
| The consent, what a site calls to send an exception, and what the page is given | `src/Bravo.Core/Telemetry/` |
| The telemetry of the host: where it goes, what every item carries, the chain of exceptions and what it loses, the item, the SDK, the service that follows the consent | `src/Bravo.Desktop/Telemetry/` |
| The route of the page | `src/Bravo.Api/Telemetry/` |
| The telemetry of the page: its sender, the consent, what it tracks | `web/controllers/telemetry.ts` |
| The run, created by `Main` | `src/Bravo.Desktop/Program.cs` |
| The exception of a request that no use case foresaw | `src/Bravo.Api/Problems/UnexpectedExceptionTelemetry.cs` |
| The exception of a task that nothing awaited | `src/Bravo.Desktop/Telemetry/UnobservedTaskExceptionTelemetry.cs` |
| The failure of the engine of the webview that ends the application | `src/Bravo.Desktop/WebView/WebViewHost.cs` |
| The crash, which the reporter sends: when, and how | `src/Bravo.Desktop/Diagnostics/CrashReportSubmission.cs` |
| The failures of the pipe of the single instance | `src/Bravo.Platform.Windows/SingleInstance/HandOverListener.cs` |
| The version of the WebView2 runtime | `src/Bravo.Desktop/WebView/WebViewRuntime.cs` |
| The events of the installer: the item, the sink that sends it | `src/Bravo.Setup/Telemetry/` |

- A site calls `IExceptionTelemetry.Track` beside the record of the exception in the log, only for an exception
  that nothing foresaw. `Track` never throws.
- `UnobservedTaskExceptionTelemetry` sends while the host runs, from a thread of the pool: the runtime raises the
  exception of a task on the thread of the finalizers when it collects the task, and the send must not hold that
  thread. A task collected before the start of the host or after its stop is in the log alone; so may be one
  collected while it stops, whose send can come after the flush of the stop.
- `ExceptionTelemetryFactory` is the one place that builds an item, from the details of `ExceptionChain`, whose
  texts already lost what is personal or marked: the SDK, given an exception, sends its message and its paths
  as they are.
- `ExceptionChain.Create` opens an aggregate of one exception, for the host and for the reporter alike. The
  type, the message and the text of a report keep the aggregate: they are for the user and for support.
- The SDK is configured with `new TelemetryConfiguration()`, never `CreateDefault` or `Active`, which add the
  modules and the sampling of a server. It sends what it is given and nothing of its own.
- `HostExceptionTelemetry` is the first service of the background that the host registers: it starts first and
  stops last, after the services whose exceptions it sends. Once it stops, a change of the settings no
  longer turns its pipeline on or off: the stop flushes that pipeline, and the next start applies the option.
- A new field or property of an item keeps the name and the values of 1.0.x when 1.0.x has one.
- The page creates its sender only with a connection string and the consent. It turns it off and on through
  `config.disableTelemetry`, which the SDK reads at each item and at each send: off, a send drops what waits,
  in the session storage as well.
  `package.json` pins the SDK to 2.8.18, the version whose code this rests on: another version is checked
  again.
- `debug.logTelemetry` writes each item of the page to the console of a development build, which sends it all
  the same.
- `TelemetryContext.ForPage` gives the page the values of the host: the page and the host never differ in them.
  A test fails when an endpoint of the connection string of the releases ends with a slash, and names no value
  of it.
- `SensitiveTags.TagNames` is the list of tags that the page removes when it copies a message, in
  `web/helpers/utils.ts`: a test checks that both match.
- No test reaches a real resource. The tests of the host send to `TestIngestion`, an endpoint on the loopback
  address, which keeps what it receives; the tests of the window, to the one of
  `web/window-tests/test-ingestion.ts`. The unit tests of the page run the SDK with a request object of the
  test, which keeps what the SDK posts. The tests of the installer post to `TestIngestion` as well. A
  development build has no destination of its own. The test of the packages installs release builds, with
  telemetry off ([Installer](installer.md)).
- The program of the installer references no package, the SDK neither ([Installer](installer.md)):
  `InstallationEventEnvelope` writes the item with the names of the SDK, and `HttpInstallationEventSink`
  posts it.
- `TelemetryPipeline.SendAsync` tells a sent item from a kept one by the status that the channel reports for each
  batch that the pipeline gave it
  ([`TelemetrySerializer`](https://github.com/microsoft/ApplicationInsights-dotnet/blob/2.23.0/BASE/src/ServerTelemetryChannel/Implementation/TelemetrySerializer.cs),
  [`Transmission`](https://github.com/microsoft/ApplicationInsights-dotnet/blob/2.23.0/BASE/src/Microsoft.ApplicationInsights/Channel/Transmission.cs)):
  200 is sent. A batch that the channel sends again later from the folder reports nothing.
- The tests of the reporter read the policies under a key of the test and the settings in a folder of the test,
  and give the proxy to no process. The test of the executable sends to `TestIngestion`.
- Microsoft supports the classic SDK of Application Insights until 2027-03-31
  ([ADR 0028](../decisions/0028-telemetry-as-in-1-0-x-on-the-classic-sdk.md)).
