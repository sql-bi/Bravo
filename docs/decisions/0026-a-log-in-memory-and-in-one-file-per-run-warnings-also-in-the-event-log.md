# 0026. A log in memory and in one file per run, warnings also in the Event Log

- Status: Accepted
- Date: 2026-11-22

## Context

The host has no logging provider: its warnings and the causes of its errors reach no record, and the `traceId`
of an error leads nowhere. 1.0.x writes its warnings to the Event Log. .NET has
[no file provider](https://learn.microsoft.com/dotnet/core/extensions/logging-providers); without a registered
source its Event Log provider writes as ".NET Runtime", and the Event Log is
[not a tracing tool](https://learn.microsoft.com/windows/win32/eventlog/logging-guidelines).

## Decision

- **Where.** The last 1000 records in memory, for the pane, and one file per run in `Logs` of the data folder:
  plain text, the 10 latest runs, 5 MB each at most, every record written at once. Warnings and errors also go
  to the Event Log, whose failures never reach the caller.
- **Levels.** Information and up of Bravo, Warning and up of the rest. The diagnostic level of 1.0.x only
  chooses what the pane shows.
- **Start.** `Main` creates the logger after the arbitration and passes it to the host
  ([ADR 0004](0004-bootstrap-services-outside-the-container.md)); a second process writes nothing.
- **Trace id.** Each record has the W3C trace id of the current activity: in a request, the `traceId` of its
  error answer; outside a request Bravo starts one for each piece of its work. A foreseen error is a warning
  with its cause. Telemetry sends the trace id of the record.
- **Page.** It reads the records at `GET /log`; its own messages stay in it.
- **Never recorded**: the token of the API, passwords, credentials, bodies and headers of requests. Bravo sends
  the log nowhere.

## Rejected alternatives

- Memory and the Event Log alone, as `main`: after a crash only the warnings remain.
- A source of its own in the Event Log: only a package for the computer can register it.
- A logging library: one more package, where a small provider does.
- A level that changes what is recorded: the file would lack what support asks for.
- The messages of the page sent to the host: its typical failure is a host that does not answer.

## Consequences

- Support finds the record of a `traceId` in one file.
- What the Event Log receives is proven on a runner.
- B-8 attaches the fatal handler to this log; B-9 sends the trace id of the record.
