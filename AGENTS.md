# AGENTS.md

Operating instructions for agents working on this repository.

## Status

Bravo for Power BI is being redesigned on this branch, `develop-vnext`, toward version 1.1.0
([ADR 0007](docs/adr/0007-redesign-on-a-development-branch.md)). Take the next item from
[docs/backlog.md](docs/backlog.md).

The code under `src/`, `test/` and `installer/` is the previous implementation. It is the reference for today's
behavior and leaves this tree when the repository layout is applied (backlog R-01). Do not extend it and do not
take its structure as a model.

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
- Build and tests pass before a push. CI does not run on pushes to this branch yet (backlog R-03): until it
  does, the local build and tests are the gate. Once it does, a red CI is fixed before anything else.
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

## The previous implementation

A WinForms shell hosts a WebView2 view for the UI and an in-process ASP.NET Core server on loopback for the API
that view calls. The .NET host lives in `src/`, the TypeScript frontend in `src/Scripts` and is built into
`src/wwwroot`. It still builds on this branch until backlog R-01 removes it; the same code is on `main`.

| Task | Command |
| --- | --- |
| Build and test | `build.cmd` |
| Build | `dotnet build Bravo.sln` |
| Test | `dotnet test test/Bravo.Tests/Bravo.Tests.csproj` |
| One test | `dotnet test test/Bravo.Tests/Bravo.Tests.csproj --filter "FullyQualifiedName~<Name>"` |

The .NET build runs `npm install` and webpack for the frontend. Add `-p:ClientAssetsEnabled=false` to skip
that step: it needs no Node.js and is considerably faster, but it also skips type checking of the frontend.

`global.json` pins the SDK. The project targets `net10.0-windows`, `win-x64`. The build stamps the version from
git history, so a clone or a CI checkout must be complete: with a shallow clone the build fails or produces a
wrong version.
