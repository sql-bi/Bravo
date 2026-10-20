# 0005. A policy read failure stops the startup

- Status: Accepted
- Date: 2026-10-20

## Context

Administrators configure Bravo through group policies in the registry: they can disable telemetry, update checks,
built-in or custom templates, or force the system browser for sign-in. User settings and telemetry are created in the
same bootstrap step, and each can fail: a corrupt settings file, an unreachable telemetry configuration, a registry
key that cannot be read.

## Decision

- `BootstrapContextFactory` reads the policies eagerly. A failure propagates and the application does not start.
- A user settings failure is logged and the defaults are used.
- A telemetry failure is logged and `NullTelemetryService` is used.

## Rejected alternatives

- Treat unreadable policies as not configured: Bravo would start with the restrictions an administrator imposed
  silently removed, for example sending telemetry where a policy forbids it.
- Stop the startup on any bootstrap failure: a corrupt settings file or a telemetry problem would make Bravo
  unusable, although neither puts an administrative restriction at risk.

## Consequences

- A policy registry key that cannot be read ends the process through the global exception handler: the user sees
  the crash dialog and the report is saved, but nothing is logged or tracked, because the bootstrap services are
  not attached to the handler yet. See
  [diagnostics](../design/diagnostics.md#startup-coverage-and-fatal-reporting).
- Defaults replace user settings only for the current session; how a corrupt settings file is kept and reported to
  the user is outside this decision.
- A new policy that restricts behavior is safe by construction: it cannot be bypassed by a read failure.
