# Network and proxy

Every connection that the application makes must reach its server from inside an enterprise network, and when
it does not, the user reads why
([ADR 0025](../decisions/0025-system-proxy-followed-as-it-is-windows-credentials-first.md)).

**The proxy.** Every HTTP client of the host sends its requests through the proxy that the user chose in the
options, the setting `proxy` of 1.0.x ([settings](settings.md)):

| Setting | What the connections use |
| --- | --- |
| System, the default | The proxy that .NET finds: the settings of Windows for the user, with a manual proxy, an auto-configuration script or auto-detection, and before them the environment variables `HTTP_PROXY`, `HTTPS_PROXY`, `ALL_PROXY` and `NO_PROXY` ([`HttpClient.DefaultProxy`](https://learn.microsoft.com/dotnet/api/system.net.http.httpclient.defaultproxy)) |
| None | No proxy, also when Windows has one |
| Custom | The server of the user: its address, where a text without a scheme is an HTTP proxy; no proxy for local names when the user asks so, nor for the addresses that match an entry of the bypass list, each a regular expression. Without an address, no proxy |

- With System the application puts no proxy of its own between .NET and the settings of Windows. .NET then
  follows a change of those settings while the application runs, and goes on to the next proxy when a script
  names several and one does not answer: it does neither for a proxy object that is not its own.
- The setting is read when the host starts. A change of the kind of proxy, of its address or of its bypass list
  applies at the next start of the application: every connection of a run goes through the same proxy, also
  the ones of a library that takes no proxy from the application.

**Failures.** A request that does not get the answer of its server fails with an error that names the cause,
one of four, the same for every connection of the application ([API](api.md)):

| Code | When | What the user is told |
| --- | --- | --- |
| `ProxyAuthenticationRequired` | The proxy server answers 407: it asks for credentials, and the ones it was given, if any, were not accepted | To enter a user name and a password in the proxy options |
| `ProxyConnectionRefused` | The proxy server answers, and refuses the connection to the server with another status | That the organization may block the address, and to turn to the administrator |
| `ServerUnreachable` | The name of the server is not known, the connection is refused or times out, or the server does not answer in time | To check the internet connection and the proxy options |
| `SecureConnectionFailed` | The certificate of the server is not trusted, as when a network inspects the connections with a certificate that the computer does not know | To turn to the administrator |

- The message of the error, in English, names the host that was asked for and nothing else of the address.
- A proxy server that cannot be reached is `ServerUnreachable`: .NET does not tell it from a server that
  cannot be reached.
- An answer of the server itself, whatever its status, is not one of these: the feature that asked decides
  what it means.
- A certificate is checked against the certificate stores of Windows, as .NET checks it for every connection:
  the application has no list of its own.

**Credentials of a proxy server.** A proxy server that asks for credentials is answered with the ones that the
user entered, when the user chose to use them and some are kept, else as the Windows user.

- The Windows user answers only Negotiate and NTLM, the schemes that send no password. A proxy server that asks
  with Basic or Digest gets nothing from the Windows user.
- The answer is decided at every challenge: a choice or an entry of the user holds from the next one, with no
  restart.
- The credentials that the user entered are one generic credential of the Windows Credential Manager, as 1.0.x
  keeps it: the user name as text, where `domain\user` names a user of a domain, and the password as its UTF-16
  characters. Its name is `Bravo for Power BI/proxy`, the one of 1.0.x; a development build keeps its own
  under `Bravo for Power BI (development)/proxy`.
- While the user has not chosen credentials of their own, nothing reads the Credential Manager.
- Credentials that cannot be read are none: the proxy server is answered as the Windows user, and the failure
  is logged.

**Limits.**

- An address of the user with a scheme that .NET takes for no proxy fails the request as a defect of the host.
- No test meets a real proxy of an enterprise, a script of an organization, or a network that inspects the
  connections: those are checked by hand.

## In the code

| Part | Path |
| --- | --- |
| The cause of a failed request, as one of the four errors | `src/Bravo.Core/Network/NetworkFailure.cs` |
| The codes | `src/Bravo.Core/Errors/ErrorCode.cs` |
| The texts of the page | `web/model/problem-texts.ts`, `web/model/i18n/en.ts` |
| The answer to a proxy server that asks for credentials, and the store of the ones of the user | `src/Bravo.Core/Network/ProxyCredentials.cs`, `IProxyCredentialStore.cs` |
| The store in the Windows Credential Manager | `src/Bravo.Platform.Windows/Network/WindowsProxyCredentialStore.cs`, `src/Bravo.Platform.Windows/Interop/CredentialStoreInterop.cs` |
| The proxy for a value of the settings, and the handler of its requests | `src/Bravo.Platform.Windows/Network/ProxySelection.cs` |
| The handler that gives a failure its code | `src/Bravo.Platform.Windows/Network/NetworkFailureHandler.cs` |
| What `Bravo.Desktop` registers: the names of the credentials, the proxy of the start, the handler of every client | `src/Bravo.Desktop/Network/` |

- `AddNetwork` gives every client of `IHttpClientFactory` its primary handler
  ([`ConfigureHttpClientDefaults`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.httpclientfactoryservicecollectionextensions.configurehttpclientdefaults)):
  a client that a component registers has the proxy, the credentials and the codes with nothing to remember. A
  component never builds an `HttpClient` or a handler of its own.
- `ProxySelection` is the one place that turns the settings into a proxy. For System it gives the handler no
  proxy object: a proxy of the application around the one of .NET would lose the changes of Windows and the
  next proxy of a script.
- `ProxySelectionStartup` makes the host read the proxy of the settings when it starts, and not at its first
  request.
- `NetworkFailureHandler` is the one place where a failure gets its code, through `NetworkFailure`, the one
  table from what .NET reports
  ([`HttpRequestError`](https://learn.microsoft.com/dotnet/api/system.net.http.httprequesterror)) to a code. A
  time-out of an `HttpClient` is raised above its handlers, where it cannot be told from a request that the
  caller cancelled: the component that owns the client reports it, with `NetworkFailure.TimedOut`, as the
  update feed does. A component that calls a library with a client of its own gives `NetworkFailure` the
  exception that the library wraps.
- The four texts are in English alone: the other languages of the page show the English one.
- `ProxyCredentials` looks at the option of the user before the store: with the default settings no code reads
  the Credential Manager, and so no test of a developer machine does.
- A test reaches no server outside the machine and asks no name to a name server: `TestProxy`, in the tests of
  `Bravo.Platform.Windows`, is a proxy on the loopback address that answers by itself, asks for the credentials
  that the test gives it and records what it was asked; the tests of `Bravo.Desktop` compile the same file.
  The addresses of the tests end in `bravo.test`, which only that proxy answers.
- A test never depends on the proxy of the machine that runs it: it names its proxy in the settings, or gives
  `ProxySelectionOptions.SystemProxy` the proxy that stands for the one of the system.
- `DesktopHostNetworkTests` builds the host as the application does, with a proxy of the user in its settings
  file, and reads the update feed through it.
- No test reads, writes or deletes a credential of the machine that runs it. The tests of the store write
  credentials of their own, named `Bravo.Tests/<identifier>/proxy`, and run only on a runner of GitHub
  Actions, which is thrown away after the run: elsewhere they are skipped. One of them reads a credential
  that `cmdkey`, a program of Windows, wrote.
- One test answers a proxy as the Windows user, with the first message of Negotiate, which holds no secret:
  the proxy of the test keeps the name of the scheme alone.
