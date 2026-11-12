# 0012. The user interface reaches the host over HTTP on loopback

- Status: Accepted
- Date: 2026-11-12

## Context

The user interface is a web application that the shell shows in a webview
([ADR 0011](0011-host-in-four-projects-with-windows-at-the-edge.md)). Two requirements decide how it reaches
the host:

- The user interface must run in an ordinary browser against a real host: a coding agent develops it and
  checks it there.
- The published Visual Studio Code extension (`sql-bi/BravoTemplateEditor` 1.0.1), which calls the host over
  HTTP, must keep working unchanged.

## Decision

For 1.1.0 the contract is an HTTP API: ASP.NET Core minimal APIs with an OpenAPI document. The shipped
application serves it on loopback: a random port on `127.0.0.1`, a token created for each process, host
filtering, and CORS restricted to the origin of the user interface. Development and production use the same
path. The decision can be revisited after 1.1.0.

References: Microsoft Learn
([Kestrel endpoints](https://learn.microsoft.com/aspnet/core/fundamentals/servers/kestrel/endpoints), for
port 0 and `IServerAddressesFeature`;
[host filtering](https://learn.microsoft.com/aspnet/core/fundamentals/servers/kestrel/host-filtering));
[RFC 8252, section 8.3](https://www.rfc-editor.org/rfc/rfc8252#section-8.3), for the loopback address written
as a literal.

## Rejected alternatives

- The message channel of the webview, with JSON-RPC on it. Blazor Hybrid, Tauri and Electron applications
  talk to their host over such a channel; here, counting the browser and the extension, it becomes two
  channels and two contracts.
- One HTTP contract reached without a port: requests intercepted in the webview
  ([`WebResourceRequested`](https://learn.microsoft.com/microsoft-edge/webview2/how-to/webresourcerequested))
  and forwarded to Kestrel over a named pipe. The interception has a precedent, the `HybridWebView` of .NET
  MAUI (`dotnet/maui`, `src/Core/src/Handlers/HybridWebView/`), but no project was found that forwards to a
  server this way, no time limit of a deferred response is documented, responses cannot stream
  ([WebView2Feedback 3519](https://github.com/MicrosoftEdge/WebView2Feedback/issues/3519)), and interception
  and named pipes tie the design to one webview engine and to Windows.

## Consequences

- A port is open on loopback while the application runs.
- The network stays in the path between the user interface and the host, so
  [issue 565](https://github.com/sql-bi/Bravo/issues/565) is not addressed at its root.
- WebView2 keeps the Local Network Access checks of Chromium off until it has an API that lets an application
  allow them ([announcement 126](https://github.com/MicrosoftEdge/WebView2Announcements/issues/126),
  [WebView2Feedback 5456](https://github.com/MicrosoftEdge/WebView2Feedback/issues/5456)).
- Revisit the decision if issue 565 proves to be network related, or if WebView2 enforces Local Network
  Access with no way for the application to allow loopback.
- Routes, errors, the client generator and long operations are designed by backlog H-03.
