# 0004. Bootstrap services are created outside the container

- Status: Accepted
- Date: 2026-10-20

## Context

Logging, policies, user settings and telemetry are needed before the ASP.NET Core host exists: the global
exception handler and the crash dialog use them, and a failure while building the host must still be reported.
The host has its own dependency injection container.

## Decision

- `BootstrapContextFactory` creates the logger factory, the policy service, the user settings and the telemetry
  service, in that order, before the host. `BootstrapContext` owns them and disposes telemetry before the logger
  factory.
- The host container receives them as existing instances, so it resolves them but never disposes them. Its default
  `ILoggerFactory` is replaced by the bootstrap one: bootstrap and host share one logging pipeline.
- No other container exists. Services that only the host needs are registered in the host container.

## Rejected alternatives

- A second `ServiceProvider` for the bootstrap: two root containers with separate lifetimes, singletons duplicated
  when a type rather than an instance is registered twice, and an ambiguous disposal order. It also turns bootstrap
  code into a service locator.
- Lazily created static services that construct one another: their creation order depends on first use, which
  caused re-entrancy, and their failures surface at an arbitrary caller.
- Registering factories in the host container: the container would dispose services that the crash path still
  needs after the host is gone.

## Consequences

- A registration in `BravoServiceCollectionExtensions` that turns one of these instances into a container-created
  service changes who disposes it and when; see
  [architecture](../design/architecture.md#composition-and-resource-ownership).
- The bootstrap order is explicit code, and each step can degrade independently; see
  [ADR 0005](0005-policy-failures-stop-startup.md).
- Tests build a `BootstrapContext` from fakes without starting the host.
