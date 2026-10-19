# AGENTS.md

Operating instructions for agents working on this repository.

## The application

Bravo for Power BI, a Windows desktop tool. A WinForms shell hosts a WebView2 view for the UI and an
in-process ASP.NET Core server on loopback for the API that view calls. The .NET host lives in `src/`, the
TypeScript frontend in `src/Scripts` and is built into `src/wwwroot`.

## Build and test

| Task | Command |
| --- | --- |
| Build and test | `build.cmd` |
| Build | `dotnet build Bravo.sln` |
| Test | `dotnet test test/Bravo.Tests/Bravo.Tests.csproj` |
| One test | `dotnet test test/Bravo.Tests/Bravo.Tests.csproj --filter "FullyQualifiedName~<Name>"` |

The .NET build runs `npm install` and webpack for the frontend. Add `-p:ClientAssetsEnabled=false` to skip
that step for a C#-only change: it needs no Node.js and is considerably faster. That flag also skips type
checking, so run a full build before finishing any change that touches `src/Scripts` or the shape of the
configuration passed to the frontend.

`global.json` pins the SDK. The project targets `net10.0-windows`, `win-x64`. Tests use xUnit and reach the
internals of `Bravo` through `InternalsVisibleTo`. The build stamps the version from git history, so a clone
or a CI checkout must be complete: with a shallow clone the build fails or produces a wrong version.

## Code

Which rule applies, in order of precedence:

1. `.editorconfig` and the analyzers, for everything they cover. Editors apply `.editorconfig`, agent file tools
   do not: write `.cs` files as UTF-8 with BOM, keep the line endings of a file you edit, and use CRLF in a new
   one. A different encoding or line ending shows the whole file as changed.
2. The Microsoft [.NET design guidelines](https://learn.microsoft.com/dotnet/standard/design-guidelines/) for
   names and API design in code you add or change, also where the surrounding code differs.
3. Code you do not otherwise change is not changed for conformance alone. Align a type when a pull request
   changes it for another reason, with the rename in its own commit.
4. Contracts that installed clients and other tools depend on win over all of the above.

## Documentation

| Location | Content |
| --- | --- |
| `docs/design/` | Current behavior that the code cannot state: invariants, ownership, external constraints, each with the consequence of breaking it |
| `docs/adr/` | One decision per file, with its reasons and rejected alternatives |

Planned work and its status are not kept in this repository.

Update a document in the same pull request that changes what it describes, and delete statements that no
longer hold. Do not add or change a rule in this file, or a decision in `docs/adr/`, unless the maintainer
asked for it.

## Read before you write

| Document | Read it before |
| --- | --- |
| [docs/design/architecture.md](docs/design/architecture.md) | changing startup, service ownership, UI threading, frontend/host contracts, API authentication, single-instance activation, or policy composition. |
| [docs/design/diagnostics.md](docs/design/diagnostics.md) | catching, logging, or reporting an exception, or touching the crash dialog, the diagnostics pane, or telemetry. |
| [docs/design/versioning.md](docs/design/versioning.md) | touching `version.json`, the build number, or any code that compares versions. |
| [ADR 0001](docs/adr/0001-releases-without-update-channels.md) | touching the update check, the update feed, or the release of a version. |
| [ADR 0002](docs/adr/0002-shared-msi-upgrade-code.md) | changing `UpgradeCode`, `MajorUpgrade` or the product version in `installer/wix`. |
