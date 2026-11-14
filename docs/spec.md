# Bravo: specification

What Bravo is, what version 1.1.0 must be, and what has been decided so far. The reasons are in
[docs/adr/](adr/); the open work is in [backlog.md](backlog.md).

## The product

Bravo for Power BI is a Windows desktop tool for authors of Power BI models. It opens a model from Power BI
Desktop, from the Power BI service or from a VPAX file and offers:

| Feature | What it does |
| --- | --- |
| Analyze Model | Shows the size of tables and columns and what is not used; imports and exports VPAX files |
| Format DAX | Formats the measures of a model |
| Manage Dates | Creates date tables and time intelligence measures from templates |
| Export Data | Exports tables to CSV and Excel files |
| Template Development | Lets template authors create and test date templates with Visual Studio Code |

Around the features: sign-in to the Power BI service, options, a diagnostics pane, a notification when a new
version exists, and the registration as an external tool of Power BI Desktop.

## Version 1.1.0

Version 1.1.0 is a redesign, not a feature release ([ADR 0007](adr/0007-redesign-on-a-development-branch.md)).

| In scope | Out of scope |
| --- | --- |
| Repository layout, host, API, installer, CI/CD, release process | New features |
| Changes to the TypeScript user interface that the new API requires | A rewrite of the TypeScript user interface |
| | Manage Calendars; a cross-platform shell |

Fixed constraints: the host runs on .NET 10, the user interface is written in TypeScript.

## Guarantees for users of 1.0.x

Whatever a component decides, these four hold:

1. A 1.0.x installation is told about 1.1.0 through the update feed it reads today.
2. 1.1.0 installs over 1.0.14 without a manual uninstall.
3. The user's settings survive the upgrade, read as they are or migrated once.
4. The policies that administrators have already deployed keep their effect.

Everything else is open to redesign: the technology of the installer, the routes of the API, the format of
files. Two points are already decided: the single instance has new names, and the arguments of the external
tool registration stay as they are ([startup](#startup-lifetime-and-single-instance)).

## Rules for every component

- **The cycle.** A component is designed before it is written: requirements, exploration, options, decision,
  implementation, result. The decision is the maintainer's and becomes an ADR of at most 400 words.
- **Requirements come from `main`.** The code on `main` is the specification of today's behavior, including
  what was learned in the field. Read it with `git show main:<path>`; run it from a worktree outside this folder
  to compare results.
- **References.** An exploration looks at well-known, well-engineered projects and at the official
  documentation, and names them.
- **Isolated components.** Code that other tools also need, such as the discovery of the local Power BI Desktop
  instances and of their ports or the report name taken from a window title, is designed as an independent
  component: its own folder and namespace, its own tests, only the .NET base libraries, everything else received
  as a parameter, a public surface designed as if it were already a package. It is not a separate project or
  package yet.
- **Networks.** Bravo is used in enterprises by people who are not technicians. Every connection it makes must
  work at the first attempt behind proxy servers, firewalls and proxy auto-configuration scripts, with no
  complex configuration.
- **The user interface in a browser.** The user interface must run in an ordinary browser against a real host:
  a coding agent develops it and checks it there.
- **The published extension.** The Visual Studio Code extension for templates, as published
  (`sql-bi/BravoTemplateEditor` 1.0.1), must keep working with 1.1.0 unchanged.

## Architecture decided so far

Each component adds its part here when its ADR is accepted.

### Repository layout

The top-level folders separate content by kind ([ADR 0008](adr/0008-repository-layout-by-kind.md)):

| Path | Content |
| --- | --- |
| `src/` | The .NET projects of the host, and nothing else |
| `tests/` | One test project per project of `src/` that has tests, at the mirrored path, with its own build rules |
| `web/` | The TypeScript user interface, self-contained, with its own `package.json` and lockfile |
| `installer/` | The installer, outside the product source |
| `policies/` | The group policy templates, which the installer ships in a subfolder of the installation |
| `docs/` | This specification, the backlog and the ADRs |
| `artifacts/` | Build and publish output of every project, the release bundle of the user interface and the results of the tests of the window, not versioned |

One solution, `Bravo.slnx`, sits at the root with the build files shared by every project. A component with
its own toolchain gets its own top-level folder; a .NET one goes into `src/` and `tests/`.

### Build rules and guardrails

A rule that a tool can check fails the build or a test, and no rule has an allow-list
([ADR 0009](adr/0009-rules-enforced-by-the-build.md)).

| Rule | Where it is |
| --- | --- |
| Nullable reference types, no implicit usings, every warning an error, the analyzers at `latest-recommended` | `Directory.Build.props` |
| Code style, naming, `sealed` on internal types, no unused using, no Windows-only API in portable code | `.editorconfig`, checked by the build |
| A project sees only the projects it references itself | `Directory.Build.props` |
| One version for each package, also where it arrives as a dependency; a lock file in each project | `Directory.Packages.props`, `packages.lock.json` |
| The clock is the injected `TimeProvider`; native declarations are generated by CsWin32, not written by hand | `BannedSymbols.txt`, `CodeAnalysis.globalconfig` |
| A project references only the projects that one table allows; no type or namespace is called Helper, Utils or Common; no static state can change; every project is listed in the solution, and in the filter built on Linux exactly when it does not target Windows | `tests/Bravo.Architecture.Tests` |
| The OpenAPI document of the API is its file in the repository; no schema allows null; every route has a name, a tag and the error answers that any route can give; every type of the API that travels is public; the codes of an error answer are the ones that the host can write | `tests/Bravo.Api.Tests` |
| The feature band of the .NET SDK; `dotnet test` in its Microsoft.Testing.Platform mode, where the tests run on xUnit v3 | `global.json` |
| The user interface has a text for every code of an error answer | `web/model/problem-texts.ts`, checked by every build of `web/` |
| The Node.js line of the user interface build, the Active LTS one | `web/package.json` |
| Text is stored with LF line endings; C# sources are checked out with CRLF on every system | `.gitattributes` |

A package with a known vulnerability is a warning and not an error, so that an advisory published later does
not break the build of an unchanged commit; it is an error only in the `audit` job of continuous integration.
No project sets the C# version: it follows the target framework. Each project sets its own target framework.

The architecture tests use reflection and read the project and solution files. Each rule is a function from its input to
the violations, tested twice: on a fixture that breaks it, which proves that the rule can fail, and on the
repository. `ProductionProjects.cs` holds the table of allowed project references.

The tests read the assemblies of `src/` as metadata, with `System.Reflection.MetadataLoadContext`, and never
load them. Their project is portable, so that it also runs on Linux, and a portable project cannot reference
one that targets Windows: its build only builds those projects first, on Windows, and gives the tests the path
of every assembly. On Windows the rules inspect the four assemblies of the host; on another system, the
portable ones.

### Continuous integration

One GitHub Actions workflow, `.github/workflows/ci.yml`, runs on every push to `main`, to `develop-vnext` and
to a work branch, on the pull requests to `main` and on request
([ADR 0010](adr/0010-continuous-integration-in-one-workflow.md)).

| Job | Runs on | What fails it |
| --- | --- | --- |
| `build-and-test` | Windows | A lock file out of date; a warning of the compiler, of an analyzer or of MSBuild; a failed test |
| `build-and-test-linux` | Linux | The same, on the projects of `Bravo.Portable.slnf`: the ones that do not target Windows |
| `build-web` | Linux | `npm ci`; a failed unit test of the user interface; the release build of the user interface, which generates the client of the host API and checks the types of the sources and of the unit tests before it bundles: a call that no longer fits the OpenAPI document fails it, and so does a type error in a test |
| `test-window` | Windows | The development bundle, the build of `Bravo.Desktop`, a failed test of the window: it keeps the report of a failed run for 3 days |
| `audit` | Windows | A NuGet package with a known vulnerability of any severity, referenced directly or not; vulnerability data that cannot be read; an npm package that ships with the user interface and has a known advisory |

`build-and-test` is the status check that `main` requires: its name does not change. `audit` runs
`npm audit` twice ([ADR 0016](adr/0016-closed-error-codes-generated-types-and-tests-in-the-user-interface.md)):
without the development dependencies, where an advisory of any severity fails the job, and on every package,
where the advisories of the build tools are reported and fail nothing. `test-window` runs the tests of the
window ([tests of the user interface](#tests-of-the-user-interface)) in a job of its own, so that the required
check stays as it is; the traces in the report of a failed run hold the token of a host whose process has
ended, and the report is kept for 3 days only. The token of a run can only read the
repository, the checkout keeps no credentials, every action is pinned to a full commit SHA, and neither the
.NET CLI nor the test platform sends usage data.

### Host

The host is four projects, with Windows at the edge
([ADR 0011](adr/0011-host-in-four-projects-with-windows-at-the-edge.md)):

| Project | Target | Content |
| --- | --- | --- |
| `Bravo.Core` | `net10.0` | Use cases, model, and the interfaces toward everything outside |
| `Bravo.Api` | `net10.0` | The contract exposed to the user interface and to the Visual Studio Code extension |
| `Bravo.Platform.Windows` | `net10.0-windows` | Adapters with no window: policies, credentials, the local Power BI Desktop |
| `Bravo.Desktop` | `net10.0-windows` | Window, webview, dialogs, composition; its executable is `Bravo.exe` |

`Bravo.Api` and `Bravo.Platform.Windows` reference only `Bravo.Core`; `Bravo.Desktop` references all three and
is the only composition root, on the .NET Generic Host. `Bravo.Core` and `Bravo.Api` use no Windows API:
dialogs, the window and the UI thread are interfaces of `Bravo.Core` that the shell implements. No state is
static or ambient: every service is injected.

The entry point, the host, the API and the main window with its webview exist.

### Startup, lifetime and single instance

`Main` runs six steps in a fixed order, on one STA thread, and returns an exit code
([ADR 0013](adr/0013-startup-in-a-fixed-order-one-instance-by-mutex-and-pipe.md)):

| Step | What happens |
| --- | --- |
| 1 | Windows Forms is configured; the DPI mode, per monitor, is a property of the project |
| 2 | An exception that escapes, on any thread, reaches one handler, and the runtime ends the process. Until backlog S-05 the handler shows a message |
| 3 | The command line becomes an activation request. The process becomes the owner of the instance, or hands the request over to the owner and ends |
| 4 | The host is built and started. It gets as instances the handler of step 2 and the ownership of step 3 |
| 5 | The main window, a service of the host, runs the message loop |
| 6 | The host is stopped and disposed, then the ownership is released |

Up to step 3 the application reads and writes nothing of its own, on the disk or in the registry, and a second
process creates no host and no window.

**Host.** It is the host of a web application, built empty: no configuration file, no environment variable, no
logging provider and no server arrives with the builder, and every registration is validated when the host is
built. The one server it has is the one of the [API](#user-interface-and-host). Its lifetime is the main
window, not a console: closing the window stops the host, and a host that stops on its own closes the window. A
stop takes at most 5 seconds, the time Windows gives an application when a session ends. A component registers
its services in an extension method of `IServiceCollection`, which only `Bravo.Desktop` calls.

**Single instance.** One instance for each user in each Windows session, at every privilege level.

- Ownership is a named mutex, scoped to the user and to the session. A process is the owner only when it has
  acquired the mutex; a mutex left by an owner that ended without releasing it is acquired.
- Hand-over is a named pipe in the same scope. A request is the version of the protocol, the length of the
  payload and the payload, a JSON object of at most 1 MiB. The owner answers with one byte: accepted, busy,
  shutting down, or a refusal. The connection and the exchange have a time limit.
- A second process that cannot deliver tells the user why and exits with code 1; it never becomes an owner. It
  tries again for 10 seconds while the owner may be starting, busy or ending. It gives up at once when access
  is denied, as between two privilege levels, and when the owner refuses the request.
- The owner starts even when it cannot create the pipe, and logs a warning.
- Before it sends, the second process grants the owner the right to come to the foreground.
- The names begin with an identifier that is new in 1.1.0: a 1.0.x instance and a 1.1.0 one do not recognize
  each other. Later versions keep the identifier; a change of the messages is a new version of the protocol,
  which an owner of another version refuses.

**Build identity.** A build that the release pipeline did not produce is a development build
([ADR 0015](adr/0015-page-under-a-reserved-host-name-api-connection-in-an-object.md)). The release pipeline
sets a build property, which reaches the application as metadata of its assembly; `Main` reads it once,
before the arbitration.

| | Release build | Development build |
| --- | --- | --- |
| Instance | The identifier of 1.1.0 | An identifier of its own: it runs beside the installed application |
| Data folder, under the local application data of the user | `Bravo` | `Bravo.Development` |
| Window title | `Bravo for Power BI - v<version>` | The same, followed by `[development build]` |

The version is the informational version of the assembly without the metadata of the build. How it is
computed is open (backlog D-02).

**Activations.** Every start is an activation request, the first one included. The requests go through one
bounded channel of 16 with a single consumer, which starts when the main window is shown: a request that
arrives earlier waits. A second process is answered busy when the channel is full, and shutting down after the
window was closed. The consumer publishes each request as an event of the API, which the page reads
([events](#user-interface-and-host)), and brings the window to the foreground.

**Command line.** `--server` or `-s`, `--database` or `-d`, and `--ppid`, with the value after a space, `=` or
`:`. The arguments of the external tool registration are unchanged:
`--server="%server%" --database="%database%"`. A start with no arguments is valid. An error never stops the
start: it travels with the request. The request carries what the command line gives; the parent process and
the report name are backlog M-01.

**Exit codes.** An exception that escapes ends the process with the code of the runtime. The application
itself ends with one of these:

| Code | When |
| --- | --- |
| 0 | The application ran and was closed, or a start was handed over |
| 1 | A hand-over failed |
| 2 | The files of the user interface are not beside the executable |
| 3 | The WebView2 runtime is installed and the webview could not be created with it |
| 4 | The processes of the webview ended more times in a row than the application recovers from |
| 1223 | The WebView2 runtime is not installed: the code that 1.0.x ends with |

### Window and webview

The main window shows the user interface in a webview, WebView2
([ADR 0015](adr/0015-page-under-a-reserved-host-name-api-connection-in-an-object.md)). Everything that knows
the engine is in one folder of `Bravo.Desktop`: a shell on another engine rewrites that folder.

**Window.** The icon of the application, the title of the
[build identity](#startup-lifetime-and-single-instance), the size that Windows gives a new window, in the
middle of the screen. The webview stays hidden until the first document has loaded: until then the window
shows the background of the user interface, so nothing flashes white. Nothing of the window is kept from one
start to the next (backlog S-01).

**Theme.** The window has the theme of the user, at the start and whenever the options change: the settings
tell that they were replaced, and the window reads them again on its own thread. The page tells the window
nothing: it changes the options.

With `Auto` the window has the color mode that Windows gives to applications, the "default app mode" of its
color settings, and follows it when it changes. It is not the mode of Windows itself, which colors the
taskbar and the Start menu: a user can have that one dark and the applications light. The application reads
the app mode where Windows keeps it for the user, the value that the engine of the webview reads to tell the
page its color scheme, so the frame and the page agree. It does not ask Windows Forms, which answers light
on every Windows before 11.

| Part | What the theme sets |
| --- | --- |
| The frame of the window, with its title bar | Dark or light, by a request to the window manager that Windows documents from Windows 11 on (build 22000). Where Windows does not know the request, the frame stays as it is |
| The background of the window and of the webview | The background of the user interface in that mode |
| The webview | The color scheme of its profile, which a page reads as `prefers-color-scheme`: the page has the theme of the user before it has read the options |

On Windows 10 the frame needs two more things, neither of them documented by Windows:

- The request has the same number from version 2004 on, and the number before it from version 1809: the
  application tries the first and, when Windows refuses it, the second, as
  [Qt](https://github.com/qt/qtbase/blob/dev/src/plugins/platforms/windows/qwindowswindow.cpp) does.
- Windows 10 goes on drawing the frame that the window had until the window gains or loses the activation.
  After a change, a window that is shown is told that its activation changed, and then its real state, as
  [Electron](https://github.com/electron/electron/blob/main/shell/browser/ui/win/electron_desktop_window_tree_host_win.cc)
  does. Windows 11 draws the new frame by itself and is told nothing.

The application calls no function that Windows exports by number alone.

**Page.** The webview serves the folder `wwwroot`, beside the executable, under the host name `bravo.invalid`,
reserved for names that are no site ([RFC 6761](https://www.rfc-editor.org/rfc/rfc6761.html#section-6.4)). No
other origin can read those files. The window opens `https://bravo.invalid/index.html`: the origin is the same
at every start, so what the page stores is found again. The engine keeps its data in the folder `WebView2` of
the data folder of the build, under the local application data of the user, and is given no browser argument.
The folder that 1.0.x used, `Bravo\.temp`, is neither read nor written (backlog D-01).

**Connection.** Before any script of a document runs, the webview runs a script that defines the object
`window.bravoHost`, with the members `address` and `token`: where the API listens, with no slash at its end,
and the token of the process. The script defines it only in a document whose origin is the origin of the page,
and the page can neither replace it nor change it. In the released application the token is never in an
address, a cookie or a file.

**What the page cannot do.** Everything that the page does not use is off: host objects, the messages of the
webview, the script dialogs and the context menus of the engine, the keys of a browser, the developer tools,
the status bar, the saving of passwords and of form data, pinch zoom, swipe navigation, and the reputation
check of addresses, which has nothing to check. A development build has the developer tools and the keys of a
browser.

- A navigation outside the origin of the page is cancelled, in the window and in a frame; a frame may hold a
  blank document.
- No window opens, and no download starts (backlog D-03).
- A permission that a document of another origin asks for is denied. For the origin of the page, reading the
  clipboard is allowed, and the permissions that the application knows and the page does not use are denied,
  each one by its name: microphone, camera, geolocation, notifications, other sensors, multiple automatic
  downloads, reading and writing files, autoplay, local fonts, MIDI system exclusive messages, window
  management, persistent storage, and the one that the engine calls unknown. None of these answers is kept
  in the profile.
- A permission that the application does not know, from a later runtime or from a later version of the
  WebView2 SDK, is not answered by the application: the engine asks the user, and keeps the answer. Denying it
  could stop the page after an update, with nothing that the user could do. A test fails when the SDK names
  a permission that the application has no answer for.

**Failures.** One type holds the rules, with no knowledge of the engine:

| Failure | What happens |
| --- | --- |
| The render process ends | The page is loaded again |
| The browser process ends | The control is disposed and a new one is created, when the engine says that the process has gone |
| Another process of the engine ends, or the page does not answer | Nothing: the engine recovers by itself, or the page may answer again |
| A fourth recovery within 5 minutes | A message, and exit code 4 |
| `wwwroot\index.html` is missing | A message, and exit code 2 |
| The runtime is not installed | A message with the address of the installer of the Evergreen runtime, and exit code 1223 |
| The engine or its control cannot be created, also because the runtime is too old for what the application uses | A message with what the engine reported, and exit code 3 |

An exception of an asynchronous step that is not a failure of the engine leaves the message loop, and ends the
application like any other. Until backlog S-05 the messages are dialogs of their own.

**In a browser.** For development and tests the same page runs in an ordinary browser, against a development
build of the host. Only a development build knows two more arguments of the command line; to a release build
they are arguments that it does not know, and so errors of the request.

| Argument | What the host does |
| --- | --- |
| `--dev-origin=<origin>` | Allows one more origin to call the API: the one of the development server |
| `--dev-connection-file=<path>` | Once it has started, writes the address and the token of the API to the file, a JSON object with the members `address` and `token`; deletes the file when it stops |

A development server serves the development bundle and the static files of the page, on the loopback address
only, rebuilds the bundle at every change, and gives the page the same object before any other script, with
the content of that file. The limit is plain: there the token is in a file and in a page that the server gives
to whoever asks for it, so whoever on the machine can read that file or reach that server can call the API of
that development host. A start that finds a development build already running is handed over to it like any
other, and what it asked for the browser has no effect.

**Local Network Access.** The page and the API have different origins, so the checks that Chromium makes on a
page that calls the loopback address apply to it. WebView2 does not make them today
([announcement 126](https://github.com/MicrosoftEdge/WebView2Announcements/issues/126)), and the application
passes no flag. What happens when they are made was measured on a development build, with version 1.0.4314.35
of the SDK and runtime 157.0.4341.46, the checks turned on by setting the environment variable
`WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS` to
`--enable-features=LocalNetworkAccessChecks,msWebViewAllowLocalNetworkAccessChecks`:

- The permission of the page reads `prompt`, and the engine raises no permission request to the application.
  The first request of the page waits, and nothing reaches the host.
- The engine asks the user with a prompt of its own: "bravo.invalid wants to: Access other apps and services
  on this device", with the buttons Block and Allow. Until the user answers, the window shows its background
  and no page.
- After Allow the page starts. At the next start with the same flags and the same data folder there is no
  prompt and the page starts: the answer is kept in the profile.
- With `msWebViewAllowLocalNetworkAccessChecks` alone the result depended on the data folder. On a new one the
  request waited and the prompt showed. On the one that the build had already used there was no prompt and
  the page called the API, while the permission still read `prompt`. The reason is not known.
- A probe outside the application, on a new data folder at every run, gave the same result with both flags and
  with the second one alone, and no effect with `LocalNetworkAccessChecks` alone.

What follows from this is backlog S-03.

### User interface and host

For 1.1.0 the user interface reaches the host through an HTTP API
([ADR 0012](adr/0012-http-on-loopback-between-user-interface-and-host.md)): ASP.NET Core minimal APIs, with the
conventions of [ADR 0014](adr/0014-api-conventions-and-a-contract-kept-as-a-file.md). Development and production
use the same path, so a browser reaches the API that the webview reaches. The Visual Studio Code extension keeps
the routes of Template Development that it calls today (backlog F-05).

**Server.** The host serves the API with Kestrel, on plain HTTP, on the loopback address `127.0.0.1` and on a
port that the system chooses at every start. A service of the host gives the address once the host has started.
The server stops with the host: a request that waits for an event is answered when the stop begins, and any
other request has the 5 seconds of the stop to end.

**Protection.** Any program and any page of the machine can reach the port.

- Every request carries the token of the process, as `Authorization: Bearer <token>`: 32 random bytes created
  at the start and kept in memory only. Without it the answer is 401, also on a path that matches no route. A
  route added later is protected with nothing to remember.
- A request names `127.0.0.1` as its host, or the answer is 400: a page that reaches the port through a name of
  its own gets nothing.
- A browser lets a page read an answer only when the page comes from an origin that the application allows.
  The application allows the origin of its page, `https://bravo.invalid`
  ([window and webview](#window-and-webview)). An allowed origin may send `GET`, `POST`, `PUT` and `DELETE`
  with the `Authorization` and `Content-Type` headers, without credentials. The answer to the request that a
  browser sends first, to ask what is allowed, may be kept for two hours, the longest that Chromium keeps it:
  without a value it is kept for 5 seconds.
- A second token is a second authentication scheme, accepted only by the routes whose policy names it: the one
  of the Visual Studio Code extension is backlog F-05.

**Routes.**

| Route | What it does |
| --- | --- |
| `GET /application` | Reads what the application is: its version, the culture of the user and whether the build is a development build |
| `GET /options` | Reads the options of the user |
| `PUT /options` | Replaces the options of the user, and returns them as they were kept |
| `GET /events?after={number}` | Reads the events of the host after a number, waiting for the next one when there is none |

- A route names a resource. An operation that is not create, read, update or delete is a `POST` on the
  resource with a verb as last segment. A `GET` changes no state, opens no dialog and starts no process.
- An operation runs inside its request, and the user interface cancels it by aborting the request. Only an
  operation that reports progress will be a resource of its own (backlog F-03).
- No route opens a dialog as a side effect: choosing a file or a folder is a request of its own, and the
  operation receives the path.
- The routes have no version: host and user interface ship together.

**JSON.**

- A member name is camelCase. An enum value and an error code are PascalCase names, and an enum never travels
  as a number.
- A number is a JSON number, never text.
- A member without a value is absent: the host never writes `null`. In a request, `null` is read as absent
  where the member is optional and is an error where the member always has a value. The document lists as
  required the members that always have a value.
- A member that the host does not know is ignored.

**Errors.** Every error answer is a problem details body ([RFC 9457](https://www.rfc-editor.org/rfc/rfc9457),
`application/problem+json`) with the member `code`: a name that never changes meaning, by which the user
interface chooses what to do. The codes are a closed list
([ADR 0016](adr/0016-closed-error-codes-generated-types-and-tests-in-the-user-interface.md)): the document
of the API lists them in the schema `ProblemCode`, and no answer carries another one.

| Status | When | `code` |
| --- | --- | --- |
| 400 | The request cannot be read, or names another host | `InvalidRequest` |
| 400 | A value is not allowed. The member `errors` names each value as it travels, such as `proxy.address` | `ValidationFailed` |
| 401 | The token of the API is missing or wrong, and nothing else: a missing sign-in to the Power BI service is a 409 with its code | `Unauthorized` |
| 404, 405, 415 | No such route, no such method on the route, a body that is not JSON | `NotFound`, `MethodNotAllowed`, `UnsupportedMediaType` |
| 409 | An error that a use case foresaw: the request was right, and the application cannot carry it out as things stand | The code of the error |
| 500 | A defect of the host. The answer tells nothing of the exception | `Unexpected` |
| Any other | The status is all that tells what happened | `HttpError` |

- A use case reports a foreseen error by throwing `BravoException` with a code and a message in English,
  which becomes the `detail` of the problem. The code is a member of one list, which admits no other value.
  No use case reports an error yet: that list is empty, and the codes of the document are the ones of the
  table.
- A route that writes a problem with a code outside the list is answered with the code of its status.

**Events.** The host tells the user interface what happens through numbered events, the first of each process
being 1. The page asks for the events after the last number it has read; the answer has the events and
`next`, the number to send with the next request.

- When there are events after the number, the answer is immediate. Otherwise the request waits: an event that
  arrives answers it at once, and after 30 seconds it is answered with no event.
- The host keeps the last 64 events. A request without a number gets all of them, so a page reads what
  happened before it was ready.
- Numbers have no gaps: when the first event of an answer does not follow the number of the request, the
  events between the two are no longer kept. A number beyond the last one is answered at once, and `next`
  brings the page back.
- An event is one type for every kind: `kind` names the kind, and the member with the data of that kind is
  the one present. The first kind is `Activation`, published at every start of the application, the first
  included, with the server, the database and the errors of the command line.

**Options.** The options are the settings of the user: theme, telemetry, update check, custom templates,
sign-in with the system browser, diagnostic level, proxy, and the options that only the user interface reads,
which the host keeps as any JSON value. A proxy address must be the address of a server, and each entry of the
bypass list a regular expression; blank text is no value. Today the settings start from their defaults at every
start: keeping them is backlog S-01, and the policies that override them backlog S-02.

**Contract.** `src/Bravo.Api/openapi.json` is the OpenAPI 3.1 document of the API. A test produces it from the
routes and fails when the file differs, so a change of the API is a change of the file in the same commit.
The application does not serve the document and ships nothing that produces it: what produces it belongs to
the tests of the API. The descriptions of the document are the comments of the types that travel, which are
public: the validation and the descriptions are generated for public types only.

Every build of the user interface generates its TypeScript client from that file, with
[Orval](https://orval.dev), into a folder that git ignores: a function for each route, named as the route is
named in the document, and the types that travel. The build then checks the user interface against them.

- Each function calls the host through one function of the user interface, which adds the address and the
  token and lets the caller abort the request.
- The codes of the errors are one of those types. The user interface has one table from code to text, and
  the build fails for a code that has no text there. For a value outside the list, which only something that
  is not this host can send, it has the text of an error that nothing describes.
- A value that travels has in the user interface its generated type, with the names of the host, and nothing
  converts a name into a number: today the theme, the diagnostic level and the type of the proxy, later the
  others, each with its feature. A number in the options that only the user interface reads stays a number:
  1.0.x saved it.
- Today the application data, the options and the events go through the client. Every other call of the user
  interface belongs to a feature that has not moved: nothing is sent, and the call fails at once with an
  error that says that the feature is not available yet. Each one moves to the client with its feature.

**Start of the page.** The page is the same in the window and in a browser, with no branch for either: it
reads the address and the token of the API from `window.bravoHost`
([window and webview](#window-and-webview)), whoever set it. A page that cannot start, because the object is
missing, the host does not answer or one of its parts fails, says so in place of the user interface: in the
language of the browser, in English where it has no text in that language, with what went wrong below and a
button that loads the page again.

- Before any of its parts exists, the page asks the API what the application is and for the options of the
  user. Until those answers arrive it follows the color scheme that it is told: in the window the theme of the
  user ([window and webview](#window-and-webview)), in a browser the color mode of the system.
- It saves no option when it starts: what it shows is what the options that it has just read say.
- It starts with no policy (backlog S-02) and no telemetry settings (backlog S-06): until then no option is
  locked, and telemetry sends nothing.
- It has no message channel with the window: what the host tells it arrives as events.
- It reads the events one request after the other. The number to ask next is kept in the session storage of
  the page, with the address of the host it belongs to: a page that is loaded again goes on from there, and
  a page of another host starts from the beginning. A page that goes away aborts its request. After an error
  the page waits before it asks again: one second, then twice as long each time, up to 30 seconds.
- An activation without a server does nothing in the page. The errors of a command line go to the diagnostic
  log. An activation with a server is logged, and opens no report until the host can find it (backlog M-01).
- Four calls that the page starts by itself have no route yet: the sign-in with the account that the page
  remembers, the update check, the reading of the diagnostics of the host, and the list of the reports that
  are open in Power BI Desktop. They fail like every call without a route, and the page shows nothing and
  writes nothing in its console for them.

### Tests of the user interface

Two kinds of tests prove the user interface
([ADR 0016](adr/0016-closed-error-codes-generated-types-and-tests-in-the-user-interface.md),
[ADR 0018](adr/0018-tests-of-the-user-interface-in-the-window-with-playwright.md)):

| Kind | Where they run | What they prove |
| --- | --- | --- |
| Unit tests, on Vitest | In Node.js, in a simulated browser | The modules that read the contract, and the start of the page, with the host replaced |
| Tests of the window, on Playwright Test | In the window of `Bravo.exe`, on Windows | The page that ships, inside the shell that ships, against its own host |

A test of the window starts the executable that the build wrote, with the development bundle beside it, and
drives its webview through the DevTools protocol. The test opens the protocol, on a port that the engine
chooses, and gives the webview a new profile, through two environment variables of the WebView2 runtime: the
application never opens the port by itself and has no option for it. Each test has an application of its
own, which it closes as a user closes it and which must end with exit code 0; no process of the application
or of its webview outlives the test, and neither does the profile.

- The page has a fixed size in every test, 1280×800, whatever the size of the window, and a test can take a
  screenshot of it. The repository keeps no reference image: the rendering changes with the machine. A
  visual comparison is made between two builds in one run (backlog H-08).
- The application has one instance: one test runs at a time, and the tests do not run together with the
  tests of the host, which start the same executable. A development build that already runs takes the start
  of a test: the test fails and says so.
- A test never reads the token of the host. A trace of a failed test holds the headers of its requests, and
  so the token of the host of that test, whose process has ended.

## Open questions

Each is answered by the component named in brackets.

- Which technology builds the installer, and how many variants are released? [installer]
- Does a per-user installation register Bravo in Power BI Desktop? Today only the per-machine one does.
  [installer]
- Is the crash reporter a mode of the main executable or an application of its own? [errors and crash
  reporting]
- How is the version computed and how are release notes and the changelog produced? [release]
