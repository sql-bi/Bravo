# Architecture and runtime boundaries

Bravo is a Windows desktop application. Its .NET process owns a WinForms window and an ASP.NET Core loopback API. The window embeds the TypeScript UI in WebView2; UI requests reach application services through the API. This document covers the boundaries that constrain changes, not individual feature implementations.

## Composition and resource ownership

[Program](../../src/Program.cs) configures the process, creates bootstrap services, installs global exception handling, then builds and runs the host. [BravoApplication](../../src/Host/BravoApplication.cs) resolves the single-instance role before starting Kestrel: a secondary process redirects activation and exits without starting its own server or window.

| Resource | Lifetime owner | Constraint for changes |
| --- | --- | --- |
| Bootstrap telemetry and logger factory | [BootstrapContext](../../src/Host/BootstrapContext.cs) | Keep them available until host disposal completes; telemetry is disposed before the logger factory |
| Bootstrap policy service and user settings | Bootstrap creates or obtains them; the container receives those instances | Preserve the shared instances so consumers do not silently use different configuration snapshots |
| Application instance and its pipe server | The host container owns `IBravoApplicationInstance`; that instance owns the pipe server | Releasing the pipe ends ownership of the application instance |
| Main window | `BravoApplication.Run` | The window is created through DI but is not registered as a container-owned service |

[BravoServiceCollectionExtensions](../../src/Host/BravoServiceCollectionExtensions.cs) registers bootstrap services as existing instances. Replacing those registrations with container-owned factories changes disposal responsibility and must preserve the ownership above. Global exception handling uses bootstrap services independently of host construction; its exact coverage is in [diagnostics](diagnostics.md#startup-coverage-and-fatal-reporting).

## UI and server threads

[AppWindow](../../src/Infrastructure/AppWindow.cs) is created on the STA entry thread, which runs the WinForms message loop. API work and single-instance activation callbacks do not have that UI-thread guarantee.

- Marshal operations on the window or WebView2 to the UI thread. Activation handlers use `ProcessHelper.InvokeOnUIThread` with the window as the control.
- Setting `SynchronizationContext.Current` is not thread dispatch. [ProcessHelper.RunWithUISynchronizationContext](../../src/Infrastructure/Helpers/ProcessHelper.cs) changes the ambient context only during a synchronous callback; do not treat it as permission to access controls from a server thread.
- Keep window activation and document delivery distinct: the window can exist before WebView2 is ready to receive a startup message.

Violating these boundaries can cause cross-thread UI failures or lost activation requests. The [single-instance contract](#single-instance-contract) describes the current startup limitation.

## Frontend and API contract

[AppWindow](../../src/Infrastructure/AppWindow.cs) injects `CONFIG` with the API address, token, options, policies and environment information. The frontend uses [Host](../../src/Scripts/controllers/host.ts) for API calls and WebView messages for desktop integration. The C# serializer and TypeScript consumers must agree on field names, nullability and enum values; changing only one side can compile in C# while breaking the UI. Follow the full-build requirement in [AGENTS.md](../../AGENTS.md#build-and-test) for changes to this contract.

[BravoApplicationBuilder](../../src/Host/BravoApplicationBuilder.cs) configures Kestrel and endpoint authorization:

| Build | Listening address | Controller authorization |
| --- | --- | --- |
| Debug | IPv4 loopback, port 5000 | No blanket authorization requirement; Swagger enabled |
| Release | IPv4 loopback, dynamically assigned port | Controllers require authorization |

[AppAuthenticationHandler](../../src/Infrastructure/Authentication/AppAuthenticationHandler.cs) requires both a loopback remote address and the application token in the `Authorization` header. The template-development token is accepted only under the template-development controller path. Loopback binding and token validation are separate checks; neither should be removed on the assumption that the other is sufficient.

Keep ProblemDetails mapping order and error contracts consistent with the frontend's interpretation of failures. Reporting responsibility at the API boundary is defined in [diagnostics](diagnostics.md#api-boundary).

## Single-instance contract

[InstancePipeName](../../src/Host/InstancePipeName.cs) identifies one instance per Windows session and user SID, independent of installation mode and elevation. Keep its application identifier, scope identifier and naming format stable across releases: changing them allows versions that share user data to run as independent owners.

[SingleInstanceServer](../../src/Infrastructure/SingleInstance/SingleInstanceServer.cs) arbitrates ownership through a named pipe with one server instance. It disconnects between clients without releasing the pipe name. Do not replace that with disposal and recreation between requests: another process could claim ownership in the gap.

Current activation limits in [BravoApplicationInstance](../../src/Host/BravoApplicationInstance.cs):

- A delivered payload does not guarantee that the requested document opened. Requests arriving before the window or WebView subscriptions are ready are not buffered.
- Activation callbacks can run concurrently and in a different order from arrival.
- Different elevation levels still share an instance name, but pipe access can prevent activation redirection. A redirect failure is logged and tracked without a user-facing dialog.

## Configuration boundaries

[PolicyService](../../src/Infrastructure/Policies/PolicyService.cs) exposes a snapshot initialized once. [RegistryPolicyReader](../../src/Infrastructure/Policies/RegistryPolicyReader.cs) merges machine and user policies, with configured machine values taking precedence. A null policy value means not configured; `false` is an explicit policy, not a request to use a default.

User preferences remain mutable through [UserPreferences](../../src/Infrastructure/Configuration/UserPreferences.cs). Do not assume that saving preferences reloads policies or that every consumer reapplies policy precedence. Host telemetry has different startup and update paths, documented in [telemetry consent](diagnostics.md#telemetry-consent).
