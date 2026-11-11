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

- Commit to `develop-vnext` and push it. No pull request and no feature branch. Never commit to `main`, never
  force-push.
- One logical change per commit, message in English, Conventional Commits.
- Build and tests pass before a push. This branch has no CI workflow yet (backlog R-03): until one runs on
  every push, the local build and tests are the gate. Once it does, a red CI is fixed before anything else.
- A component is not written before the maintainer has accepted its design. The cycle:
  1. requirements, from the behavior on `main` and the open issues;
  2. how well-known, well-engineered projects and the official documentation solve the problem;
  3. two or three options with a recommendation;
  4. the maintainer's decision, as an ADR of at most 400 words;
  5. the implementation with its tests;
  6. a report of what was proven and what must be checked by hand.
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
| `src/` | The .NET projects of the host, and nothing else | Created with the first .NET project (backlog R-02 or H-01) |
| `tests/` | One test project per project of `src/`, at the mirrored path | Created with the first .NET project (backlog R-02 or H-01) |
| `web/` | The TypeScript user interface, with its own `package.json` and lockfile | Exists |
| `installer/` | The installer | Created by backlog D-01 |
| `policies/` | The group policy templates: `bravo.admx`, `sqlbi.admx`, `en-US/` | Exists |
| `docs/` | Specification, backlog, ADRs, and the design of the previous implementation | Exists |
| `artifacts/` | Build and publish output of every project, ignored by git | Written by the build once backlog R-02 sets the output layout |
| `Bravo.slnx`, `Directory.Build.props`, `Directory.Packages.props` | The solution and the build rules of every .NET project | Created by backlog R-02 |
| `global.json`, `nuget.config`, `version.json`, `.editorconfig` | The .NET SDK, the package source, the version, the code style | Exist |

## User interface

`web/` holds the user interface of the previous implementation, moved unchanged: TypeScript and LESS, bundled by
webpack. Its commands run in `web/` and need Node.js with npm.

| Task | Command |
| --- | --- |
| Install the dependencies of the lockfile | `npm ci` |
| Release bundle into a folder | `npm run build:Release -- --output-path <folder>` |
| Development bundle into `web/wwwroot` | `npm run build:UI` |
| Development bundle, rebuilt at every change | `npm run watch:UI` |

A bundle is `js/main.js` and `css/main.css`; the release build adds `js/main.js.LICENSE.txt`. Every build type
checks the TypeScript code. `web/wwwroot` holds the static files that go with the bundle (`index.html`, icons,
fonts, images); git ignores the `js` and `css` folders that the development build adds there. Without
`--output-path` webpack writes to `web/dist`, which git does not ignore.

Until the first .NET project exists this is the only build in the tree, and the user interface has no tests.
