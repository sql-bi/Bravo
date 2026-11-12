# AGENTS.md

Operating instructions for agents working on this repository.

## Status

Bravo for Power BI is being redesigned on this branch, `develop-vnext`, toward version 1.1.0
([ADR 0007](docs/adr/0007-redesign-on-a-development-branch.md)). Take the next item from
[docs/backlog.md](docs/backlog.md).

The previous implementation, the .NET host with its tests and its installer, is not in the tree of this branch.
It is the reference for today's behavior and stays on `main`: read it with `git show main:<path>`. Do not take
its structure as a model.

## Read first

| Document | Content |
| --- | --- |
| [docs/backlog.md](docs/backlog.md) | The open work, in the order to take it |
| [docs/spec.md](docs/spec.md) | What 1.1.0 must be, the guarantees for users of 1.0.x, the rules for every component, the open questions |
| [docs/adr/](docs/adr/) | One decision per file, with its reasons and rejected alternatives |
| [docs/design/](docs/design/) | The behavior of the previous implementation: requirements and prior art for the component that replaces it |

## How the work is done on this branch

- The implementation of a component is written on a work branch, reviewed there by the maintainer, and only
  then brought to `develop-vnext`:
  1. One work branch is open at a time. It is named `vnext/<backlog id>-<slug>`, for example
     `vnext/r-02-build-rules`, starts from the tip of `develop-vnext` and lives in a worktree of its own: the
     main folder stays on `develop-vnext`.
  2. The work branch never tracks `develop-vnext`. Publish it with `git push -u origin <branch>` and check its
     upstream before any other push.
  3. The maintainer reviews `develop-vnext...<branch>`. The fixes of the review are further commits on the
     same branch.
  4. After the maintainer has accepted the branch, `develop-vnext` is fast-forwarded to its tip
     (`git merge --ff-only`) and pushed; the work branch is deleted, locally and on the remote, with its
     worktree. A rejected branch is deleted as well, not kept.
- A change without implementation, such as a backlog update, goes to `develop-vnext` directly, and not while a
  work branch is open: a fast-forward needs `develop-vnext` to stand still. If it moved all the same, the
  commits of the work branch are cherry-picked in order.
- No pull request. Never commit to `main`, never force-push.
- One logical change per commit, message in English, Conventional Commits.
- Build and tests pass before a push. CI runs on every push to `develop-vnext` and to a work branch, and a red
  CI is fixed before anything else.
- A component is not written before the maintainer has accepted its design. The cycle:
  1. requirements, from the behavior on `main` and the open issues;
  2. how well-known, well-engineered projects and the official documentation solve the problem;
  3. two or three options with a recommendation;
  4. the maintainer's decision, as an ADR of at most 400 words, the first commit of the work branch;
  5. the implementation with its tests;
  6. a report of what was proven and what must be checked by hand;
  7. the maintainer's review of the work branch, then its integration.
- One component is in progress at a time. The features do not change: an idea for a new one goes to the end of
  the backlog.
- Nothing stays only on a local machine at the end of a session.
- Update `docs/backlog.md` and `docs/spec.md` in the commit that changes what they describe, and delete
  statements that no longer hold.
- Do not add or change a rule in this file, or a decision in `docs/adr/`, unless the maintainer asked for it.

## Code

Which rule applies to new code, in order of precedence:

1. The four guarantees for users of 1.0.x in [docs/spec.md](docs/spec.md#guarantees-for-users-of-10x).
2. `.editorconfig` and the analyzers, for everything they cover. Editors apply `.editorconfig`, agent file tools
   do not: write `.cs` files as UTF-8 with BOM and with CRLF line endings. A different encoding or line ending
   shows the whole file as changed.
3. The Microsoft [.NET design guidelines](https://learn.microsoft.com/dotnet/standard/design-guidelines/) for
   names and API design.

## Repository layout

The top-level folders separate content by kind ([ADR 0008](docs/adr/0008-repository-layout-by-kind.md)). Some do
not exist yet: the table names the backlog item that creates them.

| Path | Content | State |
| --- | --- | --- |
| `src/` | The .NET projects of the host, and nothing else | Exists |
| `src/Bravo.Api/openapi.json` | The OpenAPI document of the API: a test compares it with the routes | Exists |
| `tests/` | One test project per project of `src/`, at the mirrored path, created with the first test of that project, and the architecture tests | Exists |
| `web/` | The TypeScript user interface, with its own `package.json` and lockfile | Exists |
| `installer/` | The installer | Created by backlog D-01 |
| `policies/` | The group policy templates: `bravo.admx`, `sqlbi.admx`, `en-US/` | Exists |
| `docs/` | Specification, backlog, ADRs, and the design of the previous implementation | Exists |
| `artifacts/` | Build and publish output of every project and the release bundle of the user interface, ignored by git | Written by the builds |
| `Bravo.slnx`, `Directory.Build.props`, `Directory.Packages.props` | The solution and the build rules of every .NET project | Exist |
| `Bravo.Portable.slnf` | The projects of the solution that do not target Windows, which continuous integration builds and tests on Linux | Exists |
| `global.json`, `nuget.config`, `version.json`, `.editorconfig` | The .NET SDK and the mode of `dotnet test`, the package source, the version, the code style | Exist |
| `BannedSymbols.txt`, `CodeAnalysis.globalconfig`, `.gitattributes` | The symbols no project may use, the analyzer options, the line endings | Exist |

## Projects of the host

The host is four projects, with Windows at the edge
([ADR 0011](docs/adr/0011-host-in-four-projects-with-windows-at-the-edge.md)):

| Project | Target | What goes there | May reference |
| --- | --- | --- | --- |
| `Bravo.Core` | `net10.0` | Use cases, model, and the interfaces toward everything outside | Nothing |
| `Bravo.Api` | `net10.0` | The contract exposed to the user interface and to the Visual Studio Code extension | `Bravo.Core` |
| `Bravo.Platform.Windows` | `net10.0-windows` | Adapters with no window: policies, credentials, the local Power BI Desktop | `Bravo.Core` |
| `Bravo.Desktop` | `net10.0-windows` | Window, webview, dialogs, composition; its executable is `Bravo.exe` | The other three |

- `Bravo.Core` and `Bravo.Api` use no Windows API: the build reports one as CA1416. Code that needs Windows gets
  an interface in `Bravo.Core` and its implementation in `Bravo.Platform.Windows`, or in `Bravo.Desktop` when it
  needs the window or the UI thread.
- No state is static or ambient: a service is injected, and `Bravo.Desktop` is the only place where services are
  registered.
- The user interface reaches the host through an HTTP API served on loopback
  ([ADR 0012](docs/adr/0012-http-on-loopback-between-user-interface-and-host.md)), which `Bravo.Api` holds
  ([API](#api)).
- Today `Bravo.Desktop` starts, keeps one instance, serves the API and shows an empty main window. The content
  of the window is backlog H-04.

A project of `src/` gets its test project with its first test: a run in which no test ran fails.

## Startup and single instance

`Program.Main` of `Bravo.Desktop` runs six steps in a fixed order
([ADR 0013](docs/adr/0013-startup-in-a-fixed-order-one-instance-by-mutex-and-pipe.md)), which
[docs/spec.md](docs/spec.md#startup-lifetime-and-single-instance) describes. Where each part lives:

| Part | Path |
| --- | --- |
| The order of the startup | `src/Bravo.Desktop/Program.cs` |
| The host: what it is built with, its lifetime, its stop | `src/Bravo.Desktop/Hosting/` |
| The main window, and what ties it to the host | `src/Bravo.Desktop/Shell/` |
| What is done with an exception that escapes | `src/Bravo.Desktop/Diagnostics/` |
| The command line, and the consumer of the activations | `src/Bravo.Desktop/Activation/` |
| The activation request, and the channel that every activation goes through | `src/Bravo.Core/Activation/` |
| The single instance: ownership by mutex, hand-over by pipe, arbitration | `src/Bravo.Platform.Windows/SingleInstance/` |
| The names of the instance, the listener as a service of the host, the message of a failed hand-over | `src/Bravo.Desktop/SingleInstance/` |
| The settings of the user and their store, in memory until backlog S-01 | `src/Bravo.Core/Settings/`, registered in `src/Bravo.Desktop/Settings/` |
| The Windows functions of a project, listed in its `NativeMethods.txt` and wrapped by capability | `Interop/` in `Bravo.Desktop` and in `Bravo.Platform.Windows` |

- A component registers its services in an extension method of `IServiceCollection` beside its code, and
  `DesktopHost.Build` calls it.
- `DesktopHost.Build` is the one place that names the builder, `WebApplication.CreateEmptyBuilder`: what a web
  application has by default, such as configuration files, environment variables, logging providers and a
  server, is in the host only when `Build` adds it. The API is added with `UseLoopbackServer`, `AddApi` and
  `UseApi` ([API](#api)).
- The ownership of the instance is a mutex, which only the thread that acquired it can release: `Main` acquires
  it and releases it, and a test that acquires one stays on one thread. A test that awaits holds it through
  `OwnedInstance`, on a thread of its own.
- `DesktopHostApiTests` starts the host as the application builds it, without a window, and calls the API on a
  real socket.
- The tests of the single instance use real mutexes and pipes, each test under names of its own.
  `BravoProcessTests` starts `Bravo.exe` with the names of the application: it shows a window for a moment,
  and it is skipped when an instance of this version already runs for the user.

| Task | Command |
| --- | --- |
| Run the application | `dotnet run --project src/Bravo.Desktop -c Release` |
| Run it as the external tool registration does | `dotnet run --project src/Bravo.Desktop -c Release -- --server=localhost:12345 --database=db` |
| Start the executable that the build wrote | `artifacts/bin/Bravo.Desktop/release/Bravo.exe` |

## API

`Bravo.Api` holds everything of the HTTP API that the user interface calls. Its conventions are
[ADR 0014](docs/adr/0014-api-conventions-and-a-contract-kept-as-a-file.md), and
[docs/spec.md](docs/spec.md#user-interface-and-host) describes them as they are built. Where each part lives:

| Part | Path |
| --- | --- |
| What the application calls: `AddApi`, `UseApi`, `ApiOptions` | `src/Bravo.Api/` |
| Kestrel on the loopback address, and the address it was given | `src/Bravo.Api/Server/` |
| The token and its authentication scheme, the fallback policy, host filtering, CORS | `src/Bravo.Api/Security/` |
| Problem details and their codes, and the answer to a `BravoException` | `src/Bravo.Api/Problems/` |
| How JSON is read and written | `src/Bravo.Api/Json/` |
| The events for the user interface: their log, their publisher, their route | `src/Bravo.Api/Events/` |
| The routes of a resource, with the types that travel | A folder for each resource, such as `src/Bravo.Api/Options/` |
| The OpenAPI document, and the rules applied to it when it is produced | `src/Bravo.Api/openapi.json`, `src/Bravo.Api/Contract/` |

To add a route:

1. Map it in the endpoints class of its resource, such as `OptionsEndpoints`. A new resource gets a folder, a
   class with a `Map...` method, and a call in `ApiApplicationExtensions.MapRoutes`: a route mapped there asks
   for the token and declares the answers 401 and 500 without naming them.
2. Give it `WithName`, `WithTags` and `WithSummary`. The name is the operation id, which becomes the name of the
   TypeScript function and never changes. The handler is a named static method that returns typed results;
   `ProducesValidationProblem` and `ProducesProblem` declare the error answers that the results do not show.
3. The types that travel are records in the folder of the resource: `required` on a member that always has a
   value, a nullable type on a member that may be absent. A type that a request carries is public, because the
   validation is generated for public types only. The `<summary>` of a type and of its members goes into the
   document: write it for a reader of the API, with the names of the JSON.
4. Validate with data annotations or `IValidatableObject` on the types and with attributes on the parameters,
   and test each rule through the route: nothing reports a type that the validation does not see.
5. A use case reports an error that the user interface must tell apart by throwing `BravoException`
   (`Bravo.Core.Errors`) with a new PascalCase code: the answer is 409 with that code. An error of the route
   itself is a typed problem result with its status and the member `code`.
6. Test it in `tests/Bravo.Api.Tests` on `TestApi`: the API as the application composes it, on a server in
   memory, with a clock that the test moves.
7. Write the document again and commit `src/Bravo.Api/openapi.json` with the change: until the file matches the
   routes, `OpenApiDocumentTests` fails and says so.

- An event for the user interface is a new value of `HostEventKind`, an optional member of `HostEvent` with
  its data, and a method of `IHostEventPublisher`. The kinds are not derived types: the document that .NET 10
  produces for a list of derived types is wrong.
- `AddApi` registers the services of the API and `UseLoopbackServer` its server. A use case that a route calls
  is registered by `Bravo.Desktop`, and by `TestApi` for the tests.
- `OpenApiDocumentTests.WriteTheFileOfTheRepository` is not a test: it writes the document, runs only when
  asked for, and shows as skipped in every other run.

| Task | Command |
| --- | --- |
| Write the OpenAPI document again after a change of the API | `dotnet test --project tests/Bravo.Api.Tests -c Release --explicit only` |

## Build and tests

The commands run at the root of the repository and need the .NET SDK that `global.json` names.

| Task | Command |
| --- | --- |
| Restore the packages exactly as the lock files record them | `dotnet restore Bravo.slnx --locked-mode` |
| Build | `dotnet build Bravo.slnx -c Release` |
| Test | `dotnet test --solution Bravo.slnx -c Release` |
| Test one class | `dotnet test --solution Bravo.slnx -c Release --filter-class <namespace>.<class>` |
| Test one method | `dotnet test --solution Bravo.slnx -c Release --filter-method <namespace>.<class>.<method>` |
| Update the lock files after a package change | `dotnet restore Bravo.slnx`, then commit each changed `packages.lock.json` |

The tests are xUnit v3 on Microsoft.Testing.Platform, the mode of `dotnet test` that `global.json` selects. Its
options are not those of VSTest: the solution is passed with `--solution`, and a run in which no test ran, such
as a filter that matches nothing, fails with exit code 8.

A warning fails the build ([ADR 0009](docs/adr/0009-rules-enforced-by-the-build.md)). Where the rules live:

| Rule | File |
| --- | --- |
| Compiler and analyzer settings, output folders, lock files | `Directory.Build.props`; `tests/Directory.Build.props` adds what every test project needs |
| The version of every package | `Directory.Packages.props` |
| Code style and the severity of each diagnostic | `.editorconfig` |
| The symbols no project may use | `BannedSymbols.txt` |
| Allowed project references, catch-all names, static mutable state, the projects that the solution and its filter list | `tests/Bravo.Architecture.Tests` |
| The OpenAPI document equal to its file; no schema that allows null; a name, a tag and the common error answers on every route | `tests/Bravo.Api.Tests` (`OpenApiDocumentTests`) |

ASP.NET Core is a shared framework of the runtime: a project that uses it has a `FrameworkReference` to
`Microsoft.AspNetCore.App`, and no package reference for what the framework holds, such as
`Microsoft.Extensions.Hosting`. NuGet reports such a package (NU1510), which fails the build.

A project under `src/` sets its own target framework and is named in four places:

- `Bravo.slnx`;
- `Bravo.Portable.slnf`, when it does not target Windows;
- `tests/Bravo.Architecture.Tests/ProductionProjects.cs`, the table of the projects it may reference;
- `tests/Bravo.Architecture.Tests/Bravo.Architecture.Tests.csproj`, which builds it before the tests, among the
  portable projects or among the ones that target Windows.

A test project is named in the first two, by the same condition. The architecture tests fail when a project
of `src/` or `tests/` is missing from one of these places, and when the filter lists a project that targets
Windows: a project that no solution lists would never be built or tested.

The architecture tests read the assemblies of `src/` as metadata and never load them, because their project is
portable and cannot reference a project that targets Windows. On Windows they inspect the four assemblies; on
another system, the portable ones.

## Continuous integration

`.github/workflows/ci.yml` runs on every push to `main`, to `develop-vnext` and to a work branch (`vnext/**`),
on the pull requests to `main` and on request
([ADR 0010](docs/adr/0010-continuous-integration-in-one-workflow.md)). A new push cancels the run in progress
of the same work branch or pull request; on `main` and on `develop-vnext` every pushed commit is built. A job
stops after 15 minutes.

| Job | What it runs |
| --- | --- |
| `build-and-test` | On Windows: the restore in locked mode, the build, the tests |
| `build-and-test-linux` | The same on Linux, on `Bravo.Portable.slnf`: the projects that do not target Windows |
| `build-web` | In `web/`: `npm ci`, then `npm run build:Release` |
| `audit` | A restore that fails on a NuGet package with a known vulnerability, then `npm audit` |

What a run checks beyond the commands of the table above:

- The build has `-warnaserror`. A warning in a project fails a local build as well; the switch adds the
  warnings raised outside the projects, at the level of the solution.
- `audit` restores with `-p:AuditPipeline=true`, which turns the NuGet audit warnings into errors: a package
  with a known vulnerability, and also vulnerability data that cannot be read. On a local machine the
  restore needs `--force`: without it NuGet skips a restore that is up to date, and the audit with it.
- The `npm audit` step looks at the dependencies that ship with the user interface. It reports and does not
  fail the job until they are updated (backlog H-04): its error annotation on a green run is expected.

`build-and-test` is the status check that `main` requires: do not rename the job and do not give it a matrix.
Every action is pinned to the commit of a release, with the version in a comment on the same line.

| Task | Command |
| --- | --- |
| List the runs of a branch | `gh run list --branch <branch>` |
| Wait for a run to end | `gh run watch <run id> --exit-status` |
| Read the log of the failed steps | `gh run view <run id> --log-failed` |
| Check the NuGet packages as `audit` does | `dotnet restore Bravo.slnx --force -p:AuditPipeline=true` |

The runs are also public, at <https://github.com/sql-bi/Bravo/actions>.

## User interface

`web/` holds the user interface of the previous implementation, moved unchanged: TypeScript and LESS, bundled by
webpack. Its commands run in `web/` and need Node.js 26 with npm: `package.json` pins that line in `devEngines`,
and npm stops with an error on another one.

| Task | Command |
| --- | --- |
| Install the dependencies of the lockfile | `npm ci` |
| Release bundle into `artifacts/web` | `npm run build:Release` |
| Release bundle into another folder | `npm run build:Release -- --output-path <folder>` |
| Development bundle into `web/wwwroot` | `npm run build:UI` |
| Development bundle, rebuilt at every change | `npm run watch:UI` |

A bundle is `js/main.js` and `css/main.css`; the release build adds `js/main.js.LICENSE.txt`. Every build type
checks the TypeScript code. `web/wwwroot` holds the static files that go with the bundle (`index.html`, icons,
fonts, images); git ignores the `js` and `css` folders that the development build adds there. Without
`--output-path` webpack writes to `artifacts/web` at the root of the repository, which git ignores.

The user interface has no tests.
