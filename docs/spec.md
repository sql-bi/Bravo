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
files, the name of the single-instance pipe, the arguments of the external tool registration.

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

## Architecture decided so far

Each component adds its part here when its ADR is accepted.

### Repository layout

The top-level folders separate content by kind ([ADR 0008](adr/0008-repository-layout-by-kind.md)):

| Path | Content |
| --- | --- |
| `src/` | The .NET projects of the host, and nothing else |
| `tests/` | One test project per project of `src/`, at the mirrored path, with its own build rules |
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
| A project references only the projects that one table allows; no type or namespace is called Helper, Utils or Common; no static state can change | `tests/Bravo.Architecture.Tests` |
| The feature band of the .NET SDK; `dotnet test` in its Microsoft.Testing.Platform mode, where the tests run on xUnit v3 | `global.json` |
| The Node.js line of the user interface build, the Active LTS one | `web/package.json` |
| Text is stored with LF line endings; C# sources are checked out with CRLF on every system | `.gitattributes` |

A package with a known vulnerability is a warning and not an error, so that an advisory published later does
not break the build of an unchanged commit; it is an error only in the `audit` job of continuous integration.
No project sets the C# version: it follows the target framework. Each project sets its own target framework.

The architecture tests use reflection and read the project files, with no library. Each rule is a function from
its input to the violations, tested twice: on a fixture that breaks it, which proves that the rule can fail, and
on the repository. `ProductionProjects.cs` holds the table of allowed project references, which is also the list
of the production assemblies that the rules inspect; it is empty until the host has projects.

### Continuous integration

One GitHub Actions workflow, `.github/workflows/ci.yml`, runs on every push to `develop-vnext` and to a work
branch, on the pull requests to `main` and on request
([ADR 0010](adr/0010-continuous-integration-in-one-workflow.md)). A new push cancels the run in progress of
the same branch or pull request, except on `develop-vnext`.

| Job | Runs on | What fails it |
| --- | --- | --- |
| `build-and-test` | Windows | A lock file out of date; a warning of the compiler, of an analyzer or of MSBuild; a failed test |
| `build-and-test-linux` | Linux | The same, on the whole solution until it has a Windows-only project |
| `build-web` | Linux | `npm ci` or the release build of the user interface, which also checks the types |
| `audit` | Windows | A NuGet package with a known vulnerability of any severity, referenced directly or not; vulnerability data that cannot be read |

`build-and-test` is the status check that `main` requires: its name does not change. `audit` also runs
`npm audit` on the dependencies that ship with the user interface; that step reports and does not fail the job
until they are updated. The token of a run can only read the repository, the checkout keeps no credentials,
every action is pinned to a full commit SHA, and neither the .NET CLI nor the test platform sends usage data.

## Open questions

Each is answered by the component named in brackets.

- Which technology builds the installer, and how many variants are released? [installer]
- Does a per-user installation register Bravo in Power BI Desktop? Today only the per-machine one does.
  [installer]
- Does the user interface keep calling the host over HTTP on loopback, or through the message channel of the
  webview? [host architecture]
- Is the crash reporter a mode of the main executable or an application of its own? [errors and crash
  reporting]
- Does Template Development keep its current routes for the Visual Studio Code extension, or does the
  extension change with the API? [features]
- How is the version computed and how are release notes and the changelog produced? [release]
