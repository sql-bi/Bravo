# Architecture and runtime boundaries

## Composition and resource ownership

[Program.Main](../../src/Program.cs) resolves the single-instance role right after installing the exception handler,
before every other startup step; see the [single-instance contract](#single-instance-contract).

| Resource | Lifetime owner | Constraint for changes |
| --- | --- | --- |
| Pipe server that holds the application instance | `Program.Main`, which claims it through [BravoSingleInstance](../../src/Host/BravoSingleInstance.cs) and disposes it last; the container receives it as an existing instance | Releasing the pipe ends ownership of the application instance and lets the next process become the primary. Releasing it before the bootstrap services are disposed lets two processes write the same user data |
| Activation events | The host container owns [BravoApplicationInstance](../../src/Host/BravoApplicationInstance.cs), registered as `IInstanceEvents` | Its disposal only unsubscribes from the pipe server; it must not release the pipe |
| Bootstrap telemetry and logger factory | [BootstrapContext](../../src/Host/BootstrapContext.cs) | Keep them available until host disposal completes; the exception handler is detached first, then telemetry is disposed before the logger factory |
| Bootstrap policy service and user settings | Bootstrap creates or obtains them; the container receives those instances | Preserve the shared instances so consumers do not silently use different configuration snapshots |
| Main window | `BravoApplication.Run` | The window is created through DI but is not registered as a container-owned service |

[BravoServiceCollectionExtensions](../../src/Host/BravoServiceCollectionExtensions.cs) registers the bootstrap
services and the pipe server as existing instances, which the container does not dispose. Replacing them with
container-owned factories moves disposal to the container and must keep the order above. Global exception handling
uses the bootstrap services, not the container; its coverage is in
[diagnostics](diagnostics.md#startup-coverage-and-fatal-reporting).
Why the bootstrap services live outside the container:
[ADR 0004](../adr/0004-bootstrap-services-outside-the-container.md).

## UI and server threads

[AppWindow](../../src/Infrastructure/AppWindow.cs) is created on the STA entry thread, which runs the WinForms
message loop. API work and single-instance activation callbacks do not have that UI-thread guarantee.

- Marshal operations on the window or WebView2 to the UI thread. Activation handlers use
  `ProcessHelper.InvokeOnUIThread` with the window as the control.
- Setting `SynchronizationContext.Current` is not thread dispatch.
  [ProcessHelper.RunWithUISynchronizationContext](../../src/Infrastructure/Helpers/ProcessHelper.cs) changes the
  ambient context only during a synchronous callback; do not treat it as permission to access controls from a
  server thread.
- Keep window activation and document delivery distinct: the window can exist before WebView2 is ready to receive
  a startup message.

Violating these boundaries can cause cross-thread UI failures or lost activation requests. The
[single-instance contract](#single-instance-contract) describes the current startup limitation.

## Frontend and API contract

[AppWindow](../../src/Infrastructure/AppWindow.cs) injects `CONFIG` with the API address, token, options, policies
and environment information. The frontend uses [Host](../../src/Scripts/controllers/host.ts) for API calls and
WebView messages for desktop integration. The C# serializer and TypeScript consumers must agree on field names,
nullability and enum values; changing only one side can compile in C# while breaking the UI. Follow the full-build
requirement in [AGENTS.md](../../AGENTS.md#build-and-test) for changes to this contract.

[BravoApplicationBuilder](../../src/Host/BravoApplicationBuilder.cs) configures Kestrel and endpoint authorization:

| Build | Listening address | Controller authorization |
| --- | --- | --- |
| Debug | IPv4 loopback, port 5000 | No blanket authorization requirement; Swagger enabled |
| Release | IPv4 loopback, dynamically assigned port | Controllers require authorization |

[AppAuthenticationHandler](../../src/Infrastructure/Authentication/AppAuthenticationHandler.cs) requires both a
loopback remote address and the application token in the `Authorization` header. The template-development token is
accepted only under the template-development controller path. Loopback binding and token validation are separate
checks; neither should be removed on the assumption that the other is sufficient.

[HostingExtensions.AddAndConfigureProblemDetails](../../src/Infrastructure/Extensions/HostingExtensions.cs) maps
exceptions to responses in order, first match wins. The `SocketException` mapping also matches other exception types
that wrap a socket failure, such as `AMO.ConnectionException`, so it stays after the specific mappings and before the
catch-all. Reporting at the API boundary is in [diagnostics](diagnostics.md#api-boundary).

## Single-instance contract

[InstancePipeName](../../src/Host/InstancePipeName.cs) identifies one instance per Windows session and user SID,
independent of installation mode and elevation. Keep its application identifier, scope identifier and naming format
stable across releases: changing them allows versions that share user data to run as independent owners.

[SingleInstanceServer](../../src/Infrastructure/SingleInstance/SingleInstanceServer.cs) arbitrates ownership
through a named pipe with one server instance. It disconnects between clients without releasing the pipe name. Do
not replace that with disposal and recreation between requests: another process could claim ownership in the gap.

`BravoSingleInstance.ClaimOrRedirect` claims the pipe before the bootstrap: a secondary process forwards its startup
arguments and exits without creating directories, bootstrap services, a host or a window, without reading the user
settings file, and without running the `AppEnvironment` static constructor. Keep the arbitration and the activation
payload free of those steps: a secondary that builds bootstrap services can transmit stored telemetry and reads files
that the primary writes, and the `AppEnvironment` static constructor probes the WebView2 runtime, the deployment mode
and the registry. The payload code does not reference `AppEnvironment` and takes the Windows session from the
current process.

Current activation limits:

- A delivered payload does not guarantee that the requested document opened. Requests are not buffered: the pipe
  server drops those that arrive before the host creates the `BravoApplicationInstance` that subscribes to it, and
  the window drops those that arrive before its subscriptions are ready. A request that arrives while the primary
  shuts down is lost as well, because the pipe is held until the bootstrap services are disposed.
- Activation callbacks can run concurrently and in a different order from arrival.
- Different elevation levels still share an instance name, but pipe access can prevent activation redirection. The
  secondary logs a redirect failure to the Event Log through a logger factory that has only that provider, and exits
  without a user-facing dialog. It tracks no telemetry: consent needs the policies and settings that the secondary
  does not create.

## Configuration boundaries

[PolicyService](../../src/Infrastructure/Policies/PolicyService.cs) exposes a snapshot initialized once.
[RegistryPolicyReader](../../src/Infrastructure/Policies/RegistryPolicyReader.cs) merges machine and user policies,
with configured machine values taking precedence. A null policy value means not configured; `false` is an explicit
policy, not a request to use a default.

User preferences remain mutable through [UserPreferences](../../src/Infrastructure/Configuration/UserPreferences.cs),
and saving them does not reload policies. A consumer that combines a policy with a preference applies the precedence
itself, as [TelemetryConsent](../../src/Infrastructure/Telemetry/TelemetryConsent.cs) does for telemetry.
