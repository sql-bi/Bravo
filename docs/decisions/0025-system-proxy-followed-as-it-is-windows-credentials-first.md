# 0025. The system proxy followed as it is, Windows credentials first

- Status: Accepted
- Date: 2026-11-26

## Context

Every connection must work at the first attempt behind proxy servers, scripts and firewalls
([specification](../spec.md#rules-for-every-component)). 1.0.x keeps a proxy choice, None, System or Custom, and
credentials in the Windows Credential Manager. .NET finds the
[system proxy](https://learn.microsoft.com/dotnet/api/system.net.http.httpclient.defaultproxy), sends
[credentials](https://learn.microsoft.com/dotnet/api/system.net.http.socketshttphandler.defaultproxycredentials)
only when given, and fails over among proxies only for
[its own proxy object](https://github.com/dotnet/runtime/blob/9adc3b093f764b104046918bfabc06319a3200f6/src/libraries/System.Net.Http/src/System/Net/Http/SocketsHttpHandler/HttpConnectionPoolManager.cs#L390).
The webview follows Windows and never proxies a loopback address
([Chromium](https://chromium.googlesource.com/chromium/src/+/HEAD/net/docs/proxy.md#Implicit-bypass-rules)); its
one remote server is the endpoint of the telemetry.

## Decision

- **One place.** One component gives every HTTP client of the host its handler, and the process its default
  proxy: System leaves the system proxy of .NET in place, None uses no proxy, Custom uses the user's address and
  bypass list. A change applies at the next start.
- **Credentials.** The Windows user first, for Negotiate and NTLM only; with custom credentials, the ones kept
  under the name of 1.0.x, used once entered. Their dialog opens only on request: from the options, or after a
  407.
- **Webview.** The setting as browser arguments, none for System. The engine answers Negotiate and NTLM
  itself; the user's credentials go only to the proxy of the endpoint, other requests are cancelled.
- **Failures.** One error code per cause; a proxy asking for credentials is told also at the start.
- **Names of 1.0.x.** `proxy` in the file and in `/options` is unchanged.
- **Local Network Access.** No code until a stable WebView2 SDK has the permission of
  [announcement 126](https://github.com/MicrosoftEdge/WebView2Announcements/issues/126).

## Rejected alternatives

- An application proxy object around the system one: .NET no longer fails over.
- A proxy changed while running: handlers replaced under requests in flight; a library's own client cannot
  follow.
- No browser argument: the telemetry of the page would ignore None and Custom.
- Windows credentials behind an administrator's switch, as
  [Power BI Desktop](https://learn.microsoft.com/power-bi/connect-data/desktop-troubleshooting-sign-in#using-default-system-credentials-for-web-proxy):
  [Chromium](https://www.chromium.org/developers/design-documents/http-authentication/) sends them to a proxy
  by default.
- A connectivity check at the start: the network status is
  [only a hint](https://learn.microsoft.com/uwp/api/windows.networking.connectivity.networkinformation).

## Consequences

- With System, the proxy that Windows names can ask for the user's Windows identity.
- The bypass list of the engine has no regular expressions: Custom exclusions are applied to the endpoint alone
  (backlog B-36).
- Microsoft discourages
  [browser arguments](https://learn.microsoft.com/microsoft-edge/webview2/concepts/webview-features-flags) in
  production.
