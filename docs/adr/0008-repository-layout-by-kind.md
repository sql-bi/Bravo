# 0008. The repository is laid out by kind of content

- Status: Accepted
- Date: 2026-11-11

## Context

The repository holds a .NET host with its tests, a TypeScript user interface, an installer and the group
policy templates. On `main` the user interface and the templates live inside the host project, and the .NET
build runs npm. Every later component is placed by the layout.

## Decision

The top-level folders separate content by kind:

- `src/`: the .NET projects of the host, and nothing else.
- `tests/`: one test project per project of `src/`, at the mirrored path, with its own build rules.
- `web/`: the TypeScript user interface, self-contained, with its own `package.json` and lockfile. No root
  `package.json` and no npm workspaces until a second JavaScript package exists.
- `installer/`: the installer, outside the product source.
- `policies/`: `bravo.admx`, `sqlbi.admx` and `en-US/`. The installer will ship them in a subfolder of the
  installation.
- `docs/`: specification, backlog and ADRs.
- `artifacts/`: build and publish output of every project, not versioned.

One solution, `Bravo.slnx`, sits at the root beside `Directory.Build.props` and `Directory.Packages.props`;
build output follows the .NET artifacts output layout. A future component with its own toolchain, such as a
Visual Studio Code extension, gets its own top-level folder; a .NET one goes into `src/` and `tests/`.

References: `dotnet/eShop` and `microsoft/aspire` (sibling `tests/`, `.slnx`, centralized output; aspire keeps
its Visual Studio Code extension in a top-level `extension/`), `microsoft/terminal` (top-level `policies/`),
PowerShell and Visual Studio Code (policy templates shipped in the installation folder), `Sonarr/Sonarr`
(`frontend/` at the root), and Microsoft Learn:
[Customize the build by folder](https://learn.microsoft.com/visualstudio/msbuild/customize-by-directory) and
[Artifacts output layout](https://learn.microsoft.com/dotnet/core/sdk/artifacts-output).

## Rejected alternatives

- One folder per component, with its sources and tests inside: no .NET reference repository that was read uses
  it, paths get deeper, and shared .NET code has no home.
- Everything under `src/`, with tests beside the code: toolchains are mixed in one flat list, and test-only
  rules need conditions on project names.

## Consequences

- The layout splits by toolchain, so one change can touch several top-level folders.
- If unrelated .NET products ever joined the repository, `src/` would need one more level.
- The previous host, its tests and its installer leave the tree of `develop-vnext`; they are read with
  `git show main:<path>`.
- `src/`, `tests/`, `installer/` and the root build files are created by the backlog items that need them.
