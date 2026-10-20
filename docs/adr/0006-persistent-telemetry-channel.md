# 0006. Telemetry is sent through a persistent channel

- Status: Accepted
- Date: 2026-10-20

## Context

Host telemetry carries exceptions, and the most valuable ones are tracked right before the process ends. With the
in-memory channel a transmission that fails or is still pending at exit is lost, and `Flush` gives no confirmation
of delivery. Consent comes from a policy or from the user preference and can change while Bravo runs.

## Decision

- `TelemetryService` uses `ServerTelemetryChannel` with a local store under `.cache\.telemetry` in the application
  data folder. Transmissions that cannot be sent are persisted and retried at a later start.
- Every path that ends the process or confirms a report awaits `FlushAsync` with a timeout, because the
  synchronous `Flush` returns before anything is sent.
- `TelemetryConsent.Resolve` decides whether telemetry is enabled: a configured policy wins over the user
  preference. Every caller that enables or disables telemetry resolves the consent first.

## Rejected alternatives

- Keep the in-memory channel and call `Flush` after each exception: the call blocks on a network request, it
  cannot report a failure, and anything still pending at exit is lost.
- Send crash reports through a separate endpoint or file upload: a second transport, with its own consent handling,
  for data that the telemetry pipeline already carries.

## Consequences

- An exception tracked offline reaches the service at a later start instead of being lost.
- `FlushAsync` reports that data left the process, sent or persisted, not that it was ingested.
- Transmissions in the store are sent even after telemetry is disabled: they were generated while telemetry was
  enabled and consent was given.
- The store folder must exist before the channel starts; `BravoApplicationConfiguration` creates it.
