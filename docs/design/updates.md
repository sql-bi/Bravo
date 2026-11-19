# Updates

The application tells the user that a newer release exists, and the default browser of the user downloads its
file ([ADR 0022](../decisions/0022-updates-read-from-the-feed-of-1-0-x-and-downloaded-by-the-browser.md)).
Nothing is installed by the application: the user runs the file.

**The feed.** The host reads the update feed that an installed 1.0.x reads, `currentversion.json` in the
container `bravo-public` ([release](release.md)), with `nocache=` and the seconds since 1970 in its query, so
that no cache answers with an older copy. It decodes the answer as text: by the charset that the answer names,
else by its byte order mark, else as UTF-8. The file of today is UTF-16 with a byte order mark; the one that a
release writes is UTF-8 with none. Of the JSON object the host reads three members and ignores the others:

| Member | What the host reads |
| --- | --- |
| `version` | A version of two to four numbers, as [`System.Version`](https://learn.microsoft.com/dotnet/api/system.version.tryparse) reads it |
| `download` | The address of a page of a site that the application trusts (see the browser, below), from which the host names the file of this copy |
| `changelog` | The address of a page of a site that the application trusts, the release notes |

When the feed cannot be read, the check fails with the code `UpdateCheckFailed`: whether a newer version exists
is not known. That is an answer that is not a success, no answer within the 100 seconds that an HTTP client of
.NET waits, as 1.0.x waits, a charset that .NET does not know, or a text without the three members as the
table reads them.

**A newer release.** The version of the feed is newer when it is greater than the file version of the
application, four numbers compared one by one: a preview, `1.1.0.187`, learns of the release of its version,
`1.1.0.188`. Every check that finds a newer release plays the beep of Windows, as 1.0.x does. The version that
the user reads has three numbers.

**The file of this copy.** `download` names the package per machine that carries the runtime. The host names the
file of its own kind from it, as 1.0.x names it, so that a copy gets a file of its kind:

| Copy | File, for the feed of 1.1.0 |
| --- | --- |
| Installed per machine, with the runtime | `Bravo.1.1.0.x64.msi`, as `download` names it |
| Installed per machine, without the runtime | `Bravo.1.1.0.x64-frameworkdependent.msi` |
| Installed per user, with the runtime | `Bravo.1.1.0.x64-userinstaller.msi` |
| Installed per user, without the runtime | `Bravo.1.1.0.x64-frameworkdependent-userinstaller.msi` |
| An archive, with the runtime | `Bravo.1.1.0.x64-portable.zip` |
| An archive, without the runtime | `Bravo.1.1.0.x64-frameworkdependent-portable.zip` |

- A copy is installed per machine when the registry value `installFolder` of `HKLM\Software\SQLBI\Bravo` is the
  folder of its executable, and per user when the one of `HKCU` is. Otherwise, also when the registry cannot be
  read, it is an archive: an archive is the folder of the application, and no package wrote the value.
- Whether a copy carries the runtime is decided by its build, which writes it into the application.

**When.** The page asks at its start, when the option of the update check is on, and tells a newer release in
a notification; a failure there tells nothing. The section About of the options asks again while no newer
release is known, and says that the check failed when it fails. Nothing asks at intervals.

**The browser.** The page opens the file, the release notes and every link through the route that opens an
address in the default browser of the user. The host opens only a page of a site that the application trusts,
the sites that 1.0.x trusts: `bravo.bi`, `sqlbi.com`, `github.com`, `microsoft.com`, `daxformatter.com`,
`bravorelease.blob.core.windows.net`, `code.visualstudio.com`, `marketplace.visualstudio.com` and their
subdomains, by `http` or `https`, never a loopback address. Any other address is refused as a value that is not
allowed, and the page writes the error in its log. The webview itself still downloads nothing
([window and webview](window-and-webview.md)).

**A development build** reads no feed: its check fails with `UpdateCheckFailed`, and the options say that the
check failed. Started with `--dev-update-feed=<address>`, it reads the feed at that address, an absolute `http`
or `https` one. To a release build the argument is one that it does not know, and so an error of the request
([window and webview](window-and-webview.md)).

**Turned off.** The option of the user turns off the check at the start of the page. The policy of 1.0.x that
turns it off is not read yet (backlog B-5).

**Limits.**

- Behind a proxy that asks for credentials the check fails (backlog B-6).
- A check that fails leaves no trace in a log (backlog B-7).
- No test hears the beep, opens a browser, or reads a value that a package wrote in the registry: those are
  checked by hand on an installed copy.

## In the code

| Part | Path |
| --- | --- |
| The check: it reads the feed, compares the versions, beeps and names the file of the copy | `src/Bravo.Core/Updates/UpdateCheck.cs` |
| The name of the file of a copy | `src/Bravo.Core/Updates/UpdateFile.cs` |
| What the check needs from outside: the feed, the kind of the copy, the beep | `src/Bravo.Core/Updates/` |
| The sites that the application trusts, and the default browser as a service | `src/Bravo.Core/Browser/` |
| The feed over HTTP, and the reading of its text | `src/Bravo.Platform.Windows/Updates/HttpReleaseFeed.cs`, `ReleaseFeedDocument.cs` |
| The kind of the copy, from the registry | `src/Bravo.Platform.Windows/Updates/RegistryInstallationReader.cs` |
| The default browser | `src/Bravo.Platform.Windows/Browser/DefaultBrowserLauncher.cs` |
| The routes | `src/Bravo.Api/Releases/`, `src/Bravo.Api/Browser/` |
| The address of the feed, its HTTP client, the beep: what `Bravo.Desktop` registers | `src/Bravo.Desktop/Updates/` |
| The file version of the build, and whether it carries the runtime | `src/Bravo.Desktop/BuildIdentity.cs`, `src/Bravo.Desktop/Bravo.Desktop.csproj` |
| The page | `checkForUpdates` in `web/controllers/app.ts`, `web/view/options-dialog-about.ts` |

- The feed is read through one typed client of `IHttpClientFactory`, `HttpReleaseFeed`: the proxy of backlog B-6
  is set on every client in one place.
- The members of the feed and the names of the six files are the ones of 1.0.x, and `release/Release.psm1`
  writes the feed and checks it as 1.0.x reads it. `UpdateFile`, `ReleaseFeedDocument` and that module change
  together, and the names only when no installed copy builds them.
- `TrustedSites` is the one list of the sites. The feed is read with it, the route checks an address with it,
  and `DefaultBrowserLauncher` checks it again, so that no other caller opens another address.
- `UpdateCheck` decides whether this copy reads a feed; `HttpReleaseFeed` only reads the one that it is given.
- The file version is the one that Nerdbank.GitVersioning writes into the assembly. A build that carries the
  runtime, `SelfContained`, writes the assembly metadata `BravoSelfContained`.
- No test reaches the real feed. The tests of the host give `HttpReleaseFeed` a handler of their own. The tests
  of the window start the application with `--dev-update-feed` and a server of the test
  (`web/window-tests/update-feed.ts`) that serves the feed as the storage of the releases does, UTF-16 with a
  byte order mark: by default the feed of 1.0.14, older than any build, and none for a test that sets
  `updateFeed` to `null`.
- No test opens the browser of the machine: the test of the window that downloads answers `POST /browser/open`
  itself, with `page.route`.
