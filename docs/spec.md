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
| `artifacts/` | Build and publish output of every project and the release bundle of the user interface, not versioned |

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
| The OpenAPI document of the API is its file in the repository; no schema allows null; every route has a name, a tag and the error answers that any route can give; every type of the API that travels is public | `tests/Bravo.Api.Tests` |
| The feature band of the .NET SDK; `dotnet test` in its Microsoft.Testing.Platform mode, where the tests run on xUnit v3 | `global.json` |
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
| `build-web` | Linux | `npm ci`, or the release build of the user interface, which generates the client of the host API and checks the types: a call that no longer fits the OpenAPI document fails it |
| `audit` | Windows | A NuGet package with a known vulnerability of any severity, referenced directly or not; vulnerability data that cannot be read |

`build-and-test` is the status check that `main` requires: its name does not change. `audit` also runs
`npm audit` on the dependencies that ship with the user interface; that step reports and does not fail the job
until they are updated. The token of a run can only read the repository, the checkout keeps no credentials,
every action is pinned to a full commit SHA, and neither the .NET CLI nor the test platform sends usage data.

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

The entry point, the host, the API and an empty main window exist; the content of the window is open work
(backlog H-04).

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
window was closed. The consumer publishes each request as an event of the API and brings the window to the
foreground; the user interface that reads the event is backlog H-04.

**Command line.** `--server` or `-s`, `--database` or `-d`, and `--ppid`, with the value after a space, `=` or
`:`. The arguments of the external tool registration are unchanged:
`--server="%server%" --database="%database%"`. A start with no arguments is valid. An error never stops the
start: it travels with the request. The request carries what the command line gives; the parent process and
the report name are backlog M-01.

**Exit codes.** 0 when the application ran and was closed, and when a start was handed over; 1 when a
hand-over failed. An exception that escapes ends the process with the code of the runtime.

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
- A browser lets a page read an answer only when the page comes from an origin that the application allows. No
  origin is allowed yet: the one of the user interface is decided by backlog H-04. An allowed origin may send
  `GET`, `POST`, `PUT` and `DELETE` with the `Authorization` and `Content-Type` headers, without credentials.
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
interface chooses what to do.

| Status | When | `code` |
| --- | --- | --- |
| 400 | The request cannot be read, or names another host | `InvalidRequest` |
| 400 | A value is not allowed. The member `errors` names each value as it travels, such as `proxy.address` | `ValidationFailed` |
| 401 | The token of the API is missing or wrong, and nothing else: a missing sign-in to the Power BI service is a 409 with its code | `Unauthorized` |
| 404, 405, 415 | No such route, no such method on the route, a body that is not JSON | `NotFound`, `MethodNotAllowed`, `UnsupportedMediaType` |
| 409 | An error that a use case foresaw: the request was right, and the application cannot carry it out as things stand | The code of the error |
| 500 | A defect of the host. The answer tells nothing of the exception | `Unexpected` |

A use case reports a foreseen error by throwing `BravoException` with a code and a message in English, which
becomes the `detail` of the problem. Any other status has the code `Http` followed by its number.

**Events.** The host tells the user interface what happens through numbered events, the first of each process
being 1. The page asks for the events after the last number it has read; the answer has the events and
`next`, the number to send with the next request.

- When there are events after the number, the answer is immediate. Otherwise the request waits: an event that
  arrives answers it at once, and after 30 seconds it is answered with no event.
- The host keeps the last 64 events. A request without a number gets all of them, so a page reads what
  happened before it was ready; a reload of the page reads them again (backlog H-04).
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
named in the document, and the types that travel. The build then checks the user interface against them. Each
function calls the host through one function of the user interface, which adds the address and the token and
lets the caller abort the request. Today the options go through the client; the other calls of the user
interface are as they were, and each one moves with its feature.

## Open questions

Each is answered by the component named in brackets.

- Which technology builds the installer, and how many variants are released? [installer]
- Does a per-user installation register Bravo in Power BI Desktop? Today only the per-machine one does.
  [installer]
- Is the crash reporter a mode of the main executable or an application of its own? [errors and crash
  reporting]
- How is the version computed and how are release notes and the changelog produced? [release]
