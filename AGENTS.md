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

## Read before you write

| Document | Read it before |
| --- | --- |
| [docs/design/code-conventions.md](docs/design/code-conventions.md) | creating or editing any `.cs` file. Encoding and design rules apply from its first line, and a wrong encoding rewrites the whole file. |
| [docs/design/versioning.md](docs/design/versioning.md) | touching `version.json`, the build number, or any code that compares versions. |
| [docs/documentation-guidelines.md](docs/documentation-guidelines.md) | writing or updating anything under `docs/`. |
