# 0015. The page under a reserved host name, the API connection in an object

- Status: Accepted
- Date: 2026-11-12

## Context

[ADR 0012](0012-http-on-loopback-between-user-interface-and-host.md) and
[ADR 0014](0014-api-conventions-and-a-contract-kept-as-a-file.md) left open where the page comes from and how
it reaches the API, also in a browser.

## Decision

- **Engine.** The webview code is written for WebView2, in one place that a later shell on another engine
  rewrites.
- **Page.** The webview serves the page from the installation folder
  ([local content](https://learn.microsoft.com/microsoft-edge/webview2/concepts/working-with-local-content))
  under the reserved host name `bravo.invalid`
  ([RFC 6761](https://www.rfc-editor.org/rfc/rfc6761.html#section-6.4)): a constant origin, so what the page
  stores survives a restart.
- **Connection.** Before the page's scripts run, the host sets the API address and token in an object of the
  page
  ([`AddScriptToExecuteOnDocumentCreatedAsync`](https://learn.microsoft.com/dotnet/api/microsoft.web.webview2.core.corewebview2.addscripttoexecuteondocumentcreatedasync)),
  for that origin only. In the released application the token is never in an address, a cookie or a file.
- **Channel.** Requests and events stay on HTTP, in webview and browser alike; the API allows the page's
  origin.
- **Browser.** For development and tests, a
  [development server](https://webpack.js.org/configuration/dev-server/) serves the same bundle and sets the
  same object; a development build of the host allows its origin. The user interface expects the object,
  whoever sets it.
- **Development build.** A build not produced by the release pipeline is a development build: its own instance
  beside the installed application, its own data folder
  ([`Code - OSS`](https://github.com/microsoft/vscode/blob/main/product.json)). Only it has the browser
  options; there the token reaches the development server, readable by whoever on the machine can read that
  file or reach that server.

## Rejected alternatives

- The page served by the host from `http://127.0.0.1:<port>`: the origin changes with the port, so nothing
  stored survives a restart, and the token travels in an address
  ([RFC 6750](https://www.rfc-editor.org/rfc/rfc6750.html#section-5.3)) or a cookie, which no port isolates
  ([RFC 6265](https://www.rfc-editor.org/rfc/rfc6265.html#section-8.5)).
- Events through webview messages: a second channel, which a browser lacks; a message posted during a
  navigation is lost.
- Avalonia and its webview now: the cross-platform shell follows 1.1.0.
- An engine-neutral interface now: no second implementation to shape it.

## Consequences

- Chromium's Local Network Access checks apply to a page calling loopback from another origin. WebView2 does
  not apply them today and will offer applications a permission
  ([announcement 126](https://github.com/MicrosoftEdge/WebView2Announcements/issues/126)). Measured with the
  checks enabled by feature flags: the page's first request waits for an engine prompt; none reaches the
  host.
- Later engines must serve the folder and set the object.
