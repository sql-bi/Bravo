# 0014. API conventions and a contract kept as a file

- Status: Accepted
- Date: 2026-11-12

## Context

[ADR 0012](0012-http-on-loopback-between-user-interface-and-host.md) left routes, errors, long operations and
client generation open.

## Decision

- **Routes.** A route names a resource with an identity: options, an export, the account. Any other
  operation is a `POST` on it ending in a verb ([AIP-136](https://google.aip.dev/136)). A `GET` never changes
  state, opens a dialog or starts a process.
- **Long operations.** An operation runs inside its request and is cancelled by aborting it. Only one
  reporting progress is a resource, created, read and cancelled through its routes: first the export (backlog
  F-03).
- **Events.** One route, long-polled as in [Syncthing](https://docs.syncthing.net/rest/events-get.html): the
  page asks for events after a number; the host answers at once when it has any, or holds the request up
  to a limit, then answers none. An activation reaches a waiting page with no host-side delay.
- **Errors.** [RFC 9457](https://www.rfc-editor.org/rfc/rfc9457) problem details with a stable code, a string
  that never changes meaning. Status 401 belongs to the API token alone: an application condition, like a
  missing Power BI sign-in, is a code on another status.
- **Dialogs.** No route opens a dialog as a side effect: choosing a file or folder is a separate request,
  whose path the operation receives.
- **JSON.** Enums travel as strings.
- **Contract.** The OpenAPI 3.1 document is a repository file, produced by a test
  ([`IOpenApiDocumentProvider`](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/using-openapi-documents?view=aspnetcore-10.0))
  and compared with it. [Orval](https://orval.dev/docs/guides/fetch) generates the uncommitted TypeScript
  client from it at every build of `web/`.
- **Versions.** User interface routes have no version: they ship with the host. The four extension routes
  are a fixed group (backlog F-05).

## Rejected alternatives

- Today's 46 actions on the new base: the webview message channel stays a second channel, so an activation
  cannot be tried in a browser.
- Every long operation as a `202` with a status monitor, as the
  [Azure guidelines](https://github.com/microsoft/api-guidelines/blob/vNext/azure/Guidelines.md) ask of cloud
  services: nothing times out on loopback, and user interface scenes would be rewritten.
- Server-sent events, the push standard:
  [.NET 10 emits OpenAPI 3.1](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0),
  which cannot describe stream items
  ([3.2 can](https://github.com/OAI/OpenAPI-Specification/blob/main/versions/3.2.0.md)), so events and
  transport would be hand-written.
- Generating the document at build: the tool
  [launches the application entry point](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0),
  which takes the instance mutex.

## Consequences

- Long polling is the older technique, chosen for few events, an end-to-end typed contract and a known
  precedent.
