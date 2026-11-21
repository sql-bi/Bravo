# Network and proxy

Every connection that the application makes must reach its server from inside an enterprise network, and when
it does not, the user reads why
([ADR 0025](../decisions/0025-system-proxy-followed-as-it-is-windows-credentials-first.md)).

**Failures.** The cause of a request that does not get the answer of its server is one of four errors, the same
for every connection of the application ([API](api.md)):

| Code | When | What the user is told |
| --- | --- | --- |
| `ProxyAuthenticationRequired` | The proxy server answers 407: it asks for credentials, and the ones it was given, if any, were not accepted | To enter a user name and a password in the proxy options |
| `ProxyConnectionRefused` | The proxy server answers, and refuses the connection to the server with another status | That the organization may block the address, and to turn to the administrator |
| `ServerUnreachable` | The name of the server is not known, the connection is refused or times out, or the server does not answer in time | To check the internet connection and the proxy options |
| `SecureConnectionFailed` | The certificate of the server is not trusted, as when a network inspects the connections with a certificate that the computer does not know | To turn to the administrator |

- The message of the error, in English, names the host that was asked for and nothing else of the address.
- A proxy server that cannot be reached is `ServerUnreachable`: .NET does not tell it from a server that
  cannot be reached.

**Credentials of a proxy server.** A proxy server that asks for credentials is answered with the ones that the
user entered, when the user chose to use them and some are kept, else as the Windows user.

- The Windows user answers only Negotiate and NTLM, the schemes that send no password. A proxy server that asks
  with Basic or Digest gets nothing from the Windows user.
- The answer is decided at every challenge: a choice or an entry of the user holds from the next one.
- The credentials that the user entered are one generic credential of the Windows Credential Manager, as 1.0.x
  keeps it: the user name as text, where `domain\user` names a user of a domain, and the password as its UTF-16
  characters.
- While the user has not chosen credentials of their own, nothing reads the Credential Manager.
- Credentials that cannot be read are none: the proxy server is answered as the Windows user, and the failure
  is logged.

## In the code

| Part | Path |
| --- | --- |
| The cause of a failed request, as one of the four errors | `src/Bravo.Core/Network/NetworkFailure.cs` |
| The codes | `src/Bravo.Core/Errors/ErrorCode.cs` |
| The texts of the page | `web/model/problem-texts.ts`, `web/model/i18n/en.ts` |
| The answer to a proxy server that asks for credentials, and the store of the ones of the user | `src/Bravo.Core/Network/ProxyCredentials.cs`, `IProxyCredentialStore.cs` |
| The store in the Windows Credential Manager | `src/Bravo.Platform.Windows/Network/WindowsProxyCredentialStore.cs`, `src/Bravo.Platform.Windows/Interop/CredentialStoreInterop.cs` |

- `NetworkFailure` is the one table from what .NET reports
  ([`HttpRequestError`](https://learn.microsoft.com/dotnet/api/system.net.http.httprequesterror)) to a code. A
  component that calls a library with a client of its own gives it the exception that the library wraps.
- The four texts are in English alone: the other languages of the page show the English one.
- `ProxyCredentials` looks at the option of the user before the store: with the default settings no code reads
  the Credential Manager, and so no test of a developer machine does.
- No test reads, writes or deletes a credential of the machine that runs it. The tests of the store write
  credentials of their own, named `Bravo.Tests/<identifier>/proxy`, and run only on a runner of GitHub
  Actions, which is thrown away after the run: elsewhere they are skipped. One of them reads a credential
  that `cmdkey`, a program of Windows, wrote.
