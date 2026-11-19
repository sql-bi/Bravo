# AGENTS.md

Operating instructions for agents working on this repository.

## Status

Bravo for Power BI is being redesigned on this branch, `develop-vnext`, toward version 1.1.0
([ADR 0007](docs/decisions/0007-redesign-on-a-development-branch.md)). Take the next item from
[docs/backlog.md](docs/backlog.md).

The previous implementation, the .NET host with its tests and its installer, is not in the tree of this branch.
It is the reference for today's behavior and stays on `main`: read it with `git show main:<path>`. Do not take
its structure as a model.

## Read first

| Document | Content |
| --- | --- |
| [docs/backlog.md](docs/backlog.md) | The open work, in the order to take it; what comes after 1.1.0; the open decisions |
| [docs/spec.md](docs/spec.md) | What 1.1.0 must be, the guarantees for users of 1.0.x, the rules for every component |
| [docs/decisions/](docs/decisions/) | One decision per file, with its reasons and rejected alternatives |
| [docs/design/](docs/design/README.md) | How the redesigned system works, one document for each part: what was decided, where it lives in the code, the rules for changing it |
| [docs/design/previous/](docs/design/previous/) | The behavior of the previous implementation: requirements and prior art for the component that replaces it |
| [docs/guides/](docs/guides/) | How to do a task: add a route, see the user interface in a browser, compare the screens of two builds, make a release |
| [web/AGENTS.md](web/AGENTS.md) | The user interface: its commands, its rules, its tests |

Before changing a part of the system, read its document in `docs/design/`: the rules for changing it are
there, not here. Which kind of fact lives in which document: [docs/README.md](docs/README.md).

## How the work is done on this branch

- The implementation of a component is written on a work branch, reviewed there by the maintainer, and only
  then brought to `develop-vnext`:
  1. One work branch is open at a time. It is named `vnext/<backlog id>-<slug>`, for example
     `vnext/b-2-release`, and starts from the tip of `develop-vnext`.
  2. The work branch never tracks `develop-vnext`. Publish it with `git push -u origin <branch>` and check its
     upstream before any other push.
  3. The maintainer reviews `develop-vnext...<branch>`. The fixes of the review are further commits on the
     same branch.
  4. After the maintainer has accepted the branch, `develop-vnext` is fast-forwarded to its tip
     (`git merge --ff-only`) and pushed; the work branch is deleted, locally and on the remote. A rejected
     branch is deleted as well, not kept.
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
- One component is in progress at a time. The features do not change: an idea for a new one goes to `Later` in
  the backlog.
- Nothing stays only on a local machine at the end of a session.
- Update `docs/backlog.md`, `docs/spec.md` and the documents of `docs/design/` in the commit that changes what
  they describe, and delete statements that no longer hold.
- Do not add or change a rule in this file, or a decision in `docs/decisions/`, unless the maintainer asked
  for it.

## Code

Which rule applies to new code, in order of precedence:

1. The four guarantees for users of 1.0.x in [docs/spec.md](docs/spec.md#guarantees-for-users-of-10x).
2. `.editorconfig` and the analyzers, for everything they cover. Editors apply `.editorconfig`, agent file tools
   do not: write `.cs` files as UTF-8 with BOM and with CRLF line endings. A different encoding or line ending
   shows the whole file as changed.
3. The Microsoft [.NET design guidelines](https://learn.microsoft.com/dotnet/standard/design-guidelines/) for
   names and API design.

## Commands

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

### Run the application

| Task | Command |
| --- | --- |
| Run the application | `dotnet run --project src/Bravo.Desktop -c Release` |
| Run it as the external tool registration does | `dotnet run --project src/Bravo.Desktop -c Release -- --server=localhost:12345 --database=db` |
| Start the executable that the build wrote | `artifacts/bin/Bravo.Desktop/release/Bravo.exe` |

### API

| Task | Command |
| --- | --- |
| Write the OpenAPI document again after a change of the API | `dotnet test --project tests/Bravo.Api.Tests -c Release --explicit only` |

### Installer

| Task | Command |
| --- | --- |
| Build the four packages into `artifacts/installer/en-US` | `dotnet build installer/Bravo.Installer.wixproj -c Release -t:BuildPackages` |
| Build one package | `dotnet build installer/Bravo.Installer.wixproj -c Release -t:BuildPackage -p:BravoScope=perUser -p:BravoSelfContained=false` |
| Build the six files of a release, not signed | `dotnet build installer/Bravo.Installer.wixproj -c Release -t:BuildRelease` |
| Build on a machine without the C++ build tools | add `-p:BravoSetupNativeAot=false` |
| Read the version of the build | `pwsh installer/Get-BuildVersion.ps1` |

### Release

| Task | Command |
| --- | --- |
| Test the functions of the release | `pwsh release/tests/Test-Release.ps1` |
| Start a release on a branch | `gh workflow run release.yml --ref <branch>` |
| Try a release and publish nothing | `gh workflow run release.yml --ref <branch> -f dry-run=true` |

The steps of a release are in [docs/guides/make-a-release.md](docs/guides/make-a-release.md).

### Continuous integration

| Task | Command |
| --- | --- |
| List the runs of a branch | `gh run list --branch <branch>` |
| Wait for a run to end | `gh run watch <run id> --exit-status` |
| Read the log of the failed steps | `gh run view <run id> --log-failed` |
| Check the NuGet packages as `audit` does | `dotnet restore Bravo.slnx --force -p:AuditPipeline=true` |
| Check the npm packages as `audit` does, in `web/` | `npm audit --omit=dev`, then `npm audit` |

The runs are also public, at <https://github.com/sql-bi/Bravo/actions>.

The commands of the user interface are in [web/AGENTS.md](web/AGENTS.md).
