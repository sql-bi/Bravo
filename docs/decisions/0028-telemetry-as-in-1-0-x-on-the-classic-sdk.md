# 0028. Telemetry as in 1.0.x on the classic SDK

- Status: Accepted
- Date: 2026-11-26

## Context

1.0.x sends to Application Insights from the host, the page and the installer, under the user's option, a choice
of who installs and the policy `TelemetryEnabled`. The classic .NET SDK
[retires on 2027-03-31](https://learn.microsoft.com/previous-versions/azure/azure-monitor/app/classic-api). A
crash leaves a report, which Bravo started as its reporter shows
([ADR 0027](0027-a-crash-leaves-a-report-beside-the-log-shown-by-bravo-started-as-its-reporter.md)).

## Decision

- **Senders.** The host with the classic SDK 2.23; the page with `@microsoft/applicationinsights-web` 2.8.18, by
  itself; `Bravo.Setup.exe` with a sender of its own. The names and the context of 1.0.x stay.
- **Content.** The host sends, explicitly, the exceptions that nothing foresaw and nothing else, without
  sensitive tags or user and machine names. The trace of the record is the operation; role instance and node
  name are hashes. The page sends what it tracks and its SDK's critical errors.
- **Consent.** The policy, else the option, else the choice of who installs, else on; a change applies at once,
  in the page too. The installer follows only `INSTALLERTELEMETRYENABLED`, as in 1.0.x.
- **Crash.** The process that crashes sends nothing. The reporter sends the exception rebuilt from the report:
  by itself with consent, else once with a Send button; never against a policy.
- **Offline.** What is not sent waits in the data folder, deleted when telemetry is off.
- **Proxy.** The host, the reporter and the page send through the proxy of the settings
  ([ADR 0025](0025-system-proxy-followed-as-it-is-windows-credentials-first.md)).
- **Destination.** The connection string is in the sources; only a release build uses it. Tests send to the
  loopback address.

## Rejected alternatives

- Application Insights 3 or OpenTelemetry now: for the host a wider B-9, while the SDK stays behind
  `ITelemetryPipeline`; for the page no page views; for `Bravo.Setup.exe` no compilation ahead of time.
- The events of the page through the host: the .NET SDK 3 tracks no page views, which B-46 would lose.
- Log records: a provider is a complication, with duplicates and loops to leave out.
- The crash sent at the next start: late, or never.

## Consequences

- A runner that installs a release build passes `INSTALLERTELEMETRYENABLED=0` and forbids telemetry by policy.
- The host leaves the classic SDK before 2027-03-31 ([backlog](../backlog.md)).
