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

**The webview.** The page and the host are on one machine: the webview loads the page from the folder of the
application and calls the host on the loopback address, which the engine sends to no proxy
([Chromium](https://chromium.googlesource.com/chromium/src/+/HEAD/net/docs/proxy.md#Implicit-bypass-rules)).
The webview is given no browser argument, where 1.0.x gave it the proxy of the user: what it would load from
outside goes through the proxy of Windows, whatever the option of the user, and the engine answers by itself
a proxy server that asks for credentials. Today the page loads nothing from outside
([window and webview](window-and-webview.md)).

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
- The user enters them in the dialog box of Windows for credentials (`POST /network/proxy-credentials/prompt`,
  [API](api.md)), with the title and the text of 1.0.x, in English. The dialog box belongs to the main window,
  which takes no input until it is closed, and the request is answered then. It opens with the user name that
  is kept, if any, and what the user enters is kept in its place.
- A dialog box that the user cancels, an entry without a user name, and a request that arrives while the
  application has no window leave what was kept: the answer says that nothing was entered.
- `DELETE /network/proxy-credentials` forgets the credentials: the next challenge is answered as the Windows
  user.

**Connection test.** The user asks the application whether it reaches the sites that it needs
(`POST /network/test`, [API](api.md)). Each site is asked for an answer over HTTP, and the result tells for
each one the proxy server that the request goes through and, when the site did not answer, which of the four
causes.

| Site | The address that is asked |
| --- | --- |
| `SignIn` | `https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration` |
| `PowerBIService` | `https://api.powerbi.com/` |
| `DaxFormatter` | `https://www.daxformatter.com/` |
| `UpdateFeed` | The update feed of the build ([updates](updates.md)) |

- The test uses the proxy settings as they are kept when it runs, not the proxy of the run: a user who
  changed them tries them before the restart that applies them. The credentials are the ones of every
  connection.
- A site that answers is reached, whatever the status of its answer, which is not read. Each address answers
  by itself, and sends the request on to no other site.
- A site that does not answer is a result of the test, not an error of the route. The sites are asked at the
  same time, and a site has 30 seconds to answer.
- The proxy server of a result is the one that the settings give for the address of the site: absent when the
  request goes to the site with no proxy, and the first one where a script names several.
- The update feed is not asked when a policy turned the update check off ([policies](policies.md)): the
  application sends no request there.
- A development build asks the update feed that it was given with `--dev-update-feed` and no other site, so no
  test of the application leaves the machine.

**In the options.** The section Proxy of the options has the proxy server of the user and what goes with it.

- Below the proxy server, a note says that a change of the server, of its address or of its exclusions takes
  effect at the next start of the application.
- The switch of the custom credentials asks for them when the user turns it on, and stays off when the user
  enters none. Turned off, it asks the user to confirm, and then forgets the credentials. A link asks for them
  again; with the switch off, the link turns it on.
- A request for the credentials, or to forget them, that fails is told in a notification, and leaves the
  switch as it was.
- The connection test is a button. It saves the options of the dialog first, then shows each site with its
  name, whether it was reached or the text of its cause, and the proxy server that the request went through.
- At the start of the page, an update check that finds a proxy server asking for credentials tells it in a
  notification, with a link that opens this section. In the section About, an update check that did not reach
  the feed says its cause ([updates](updates.md)).

**Limits.**

- An address of the user with a scheme that .NET takes for no proxy fails the request as a defect of the host.
- Credentials that Windows does not keep or forget, as where an administrator forbids kept credentials, fail
  the request as a defect of the host: the user is told of an error, and not of its cause.
- Proxy settings of Windows that take the loopback address through the proxy, with the entry `<-loopback>`
  among their exceptions, keep the page from reaching the host: that entry takes away the rule by which the
  engine sends the loopback address to no proxy
  ([Chromium](https://chromium.googlesource.com/chromium/src/+/HEAD/net/docs/proxy.md#Overriding-the-implicit-bypass-rules)).
- No test meets a real proxy of an enterprise, a script of an organization, or a network that inspects the
  connections: those are checked by hand.
- No test shows the dialog box of Windows for the credentials: it is checked by hand.

## In the code

| Part | Path |
| --- | --- |
| The cause of a failed request, as one of the four errors | `src/Bravo.Core/Network/NetworkFailure.cs` |
| The codes | `src/Bravo.Core/Errors/ErrorCode.cs` |
| The texts of the page | `web/model/problem-texts.ts`, `web/model/i18n/en.ts` |
| The answer to a proxy server that asks for credentials, and the store of the ones of the user | `src/Bravo.Core/Network/ProxyCredentials.cs`, `IProxyCredentialStore.cs` |
| The store in the Windows Credential Manager | `src/Bravo.Platform.Windows/Network/WindowsProxyCredentialStore.cs`, `src/Bravo.Platform.Windows/Interop/CredentialStoreInterop.cs` |
| The user name of credentials as one text | `src/Bravo.Core/Network/CredentialUserName.cs` |
| The dialog that asks the user for the credentials, and the use case that keeps what the user enters | `src/Bravo.Core/Network/IProxyCredentialDialog.cs`, `ProxyCredentialsPrompt.cs` |
| The dialog box of Windows, over the main window | `src/Bravo.Desktop/Network/ProxyCredentialDialog.cs`, `src/Bravo.Desktop/Interop/CredentialDialogInterop.cs` |
| The proxy for a value of the settings, and the handler of its requests | `src/Bravo.Platform.Windows/Network/ProxySelection.cs` |
| The handler that gives a failure its code | `src/Bravo.Platform.Windows/Network/NetworkFailureHandler.cs` |
| The connection test, and what asks a site for it | `src/Bravo.Core/Network/ConnectionTest.cs`, `ISiteProbe.cs`, `src/Bravo.Platform.Windows/Network/HttpSiteProbe.cs` |
| The routes, with the types that travel | `src/Bravo.Api/Network/` |
| The section Proxy of the options | `web/view/options-dialog-proxy.ts` |
| The tests of the window for it, and the proxy server of a test | `web/window-tests/proxy.spec.ts`, `web/window-tests/test-proxy.ts` |
| What `Bravo.Desktop` registers: the names of the credentials, the proxy of the start, the handler of every client, the sites of the connection test | `src/Bravo.Desktop/Network/` |

- `AddNetwork` gives every client of `IHttpClientFactory` its primary handler
  ([`ConfigureHttpClientDefaults`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.httpclientfactoryservicecollectionextensions.configurehttpclientdefaults)):
  a client that a component registers has the proxy, the credentials and the codes with nothing to remember. A
  component never builds an `HttpClient` or a handler of its own.
- `HttpSiteProbe` is the one exception: the clients of the factory have the proxy of the start, and the
  connection test asks with the settings as they are kept now. It builds its client with `ProxySelection`, as
  `AddNetwork` does, so its requests have the same credentials and the same codes.
- A site of the connection test is a value of `NetworkSite` with its address in `AddNetwork`. The update feed
  is not in that list: the test takes the address from the options of the update check, the one that the
  check reads.
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
- The four texts, and the ones of the section Proxy that are new, are in English alone: the other languages of
  the page show the English one.
- The name of each site of the connection test is in a table of `options-dialog-proxy.ts`, typed by the sites
  of the generated client as the texts of the codes are: a site that the host adds stops the build of the page
  until it has a name.
- A switch of the options that asks before it changes goes back to what it was when the answer is no, and
  when the question fails (`web/helpers/renderer.ts`).
- `proxy.spec.ts` gives the application a proxy server of the test, `test-proxy.ts`, on the loopback address,
  which answers with the update feed or asks for credentials: the host reaches it as it reaches any other,
  and the page shows what the host found. A test of the credentials answers in place of the host, with
  `bravo.answerProxyCredentialRequests`: the host would show the dialog box of Windows, and would delete from
  the Credential Manager of the machine. Those tests have no proxy server, so nothing asks the host for
  credentials.
- `ProxyCredentials` looks at the option of the user before the store: with the default settings no code reads
  the Credential Manager, and so no test of a developer machine does.
- `CredentialUserName` is the one place that knows the text `domain\user`: the store keeps it, and the dialog
  box shows it and reads it.
- The dialog box of Windows is shown on the thread of the main window, which `ProxyCredentialDialog` reaches
  through `MainWindowDispatcher` ([startup](startup.md)). `CredentialDialogInterop` gives the dialog box the
  user name as a block that Windows packs, reads what the user entered from the block that Windows gives
  back, and clears that block before it frees it. Its tests pack and read a block in memory: they show
  nothing.
- A test reaches no server outside the machine and asks no name to a name server: `TestProxy`, in the tests of
  `Bravo.Platform.Windows`, is a proxy on the loopback address that answers by itself, asks for the credentials
  that the test gives it and records what it was asked; the tests of `Bravo.Desktop` compile the same file.
  The addresses of the tests end in `bravo.test`, which only that proxy answers.
- A test never depends on the proxy of the machine that runs it: it names its proxy in the settings, or gives
  `ProxySelectionOptions.SystemProxy` the proxy that stands for the one of the system.
- `DesktopHostNetworkTests` builds the host as the application does, with a proxy of the user in its settings
  file, and reads the update feed through it. It asks the connection test after a change of the options, which
  goes through the proxy that the options name while the update feed still goes through the one of the start.
- `BravoNetworkTests` starts the executable with the proxy of the system, which no test can give a host in its
  own process: .NET takes it from the variable `HTTP_PROXY` of the process before it looks at Windows. The
  test calls the API of the application, whose address and token it reads from the file of
  `--dev-connection-file`.
- No test reads, writes or deletes a credential of the machine that runs it, nor changes its Internet
  settings. The tests that do run only on a runner of GitHub Actions, which is thrown away after the run:
  elsewhere they are skipped.
  - The tests of the store write credentials of their own, named `Bravo.Tests/<identifier>/proxy`, and one
    of them reads a credential that `cmdkey`, a program of Windows, wrote.
  - `BravoSystemProxyTests` writes the Internet settings of the user with the function of Windows for them
    ([`InternetSetOption`](https://learn.microsoft.com/windows/win32/api/wininet/nf-wininet-internetsetoptionw)),
    starts the executable after that, and puts back what it found: a proxy for plain HTTP addresses alone,
    then a script that names a proxy that does not listen before the one of the test, which passes only if
    .NET goes on to the next proxy. Two more tests keep credentials under the name of a development build,
    with `cmdkey`: the proxy of the test is answered with them, and gets none after the route that forgets
    them. `cmdkey` reads its command line by itself: a name with spaces is quoted after the colon of its
    switch.
- The tests answer a proxy as the Windows user only with the first message of Negotiate, which holds no secret:
  the proxy of the test keeps the name of the scheme alone.
- The classes that start the executable are one collection of xUnit, `BravoExecutable.Collection`: the
  application has one instance, and they run one after the other.
