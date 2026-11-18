# Architecture

How the repository is laid out and which projects make the host. Each other document of this folder describes
one part of the system.

## Repository layout

The top-level folders separate content by kind ([ADR 0008](../adr/0008-repository-layout-by-kind.md)):

| Path | Content |
| --- | --- |
| `src/` | The .NET projects of the host, and nothing else |
| `tests/` | One test project per project of `src/` that has tests, at the mirrored path, with its own build rules |
| `web/` | The TypeScript user interface, self-contained, with its own `package.json` and lockfile |
| `installer/` | The installer, outside the product source |
| `policies/` | The group policy templates, which the installer ships in a subfolder of the installation |
| `docs/` | This specification, the backlog and the ADRs |
| `artifacts/` | Build and publish output of every project, the release bundle of the user interface, the installer packages, the results of the tests of the window and the screens of the window, not versioned |

One solution, `Bravo.slnx`, sits at the root with the build files shared by every project. A component with
its own toolchain gets its own top-level folder; a .NET one goes into `src/` and `tests/`.

### In the code

The top-level folders separate content by kind ([ADR 0008](../adr/0008-repository-layout-by-kind.md)). Some do
not exist yet: the table names the backlog item that creates them.

| Path | Content | State |
| --- | --- | --- |
| `src/` | The .NET projects of the host, and nothing else | Exists |
| `src/Bravo.Api/openapi.json` | The OpenAPI document of the API: a test compares it with the routes | Exists |
| `tests/` | One test project per project of `src/`, at the mirrored path, created with the first test of that project, and the architecture tests | Exists |
| `web/` | The TypeScript user interface, with its own `package.json` and lockfile | Exists |
| `installer/` | The WiX project of the installer packages, and the tests that install them ([Installer](installer.md)) | Exists |
| `policies/` | The group policy templates: `bravo.admx`, `sqlbi.admx`, `en-US/` | Exists |
| `docs/` | Specification, backlog, ADRs, and the design of the previous implementation | Exists |
| `artifacts/` | Build and publish output of every project, the release bundle of the user interface, the installer packages with the folders they are made from, the results of the tests of the window and the screens of the window, ignored by git | Written by the builds, the tests and the command of the screens |
| `Bravo.slnx`, `Directory.Build.props`, `Directory.Packages.props` | The solution and the build rules of every .NET project | Exist |
| `Bravo.Portable.slnf` | The projects of the solution that do not target Windows, which continuous integration builds and tests on Linux | Exists |
| `Directory.Build.targets` | What is done to every .NET project after its packages: the reference that the WebView2 package adds to its WPF control is removed | Exists |
| `global.json`, `nuget.config`, `version.json`, `.editorconfig` | The .NET SDK and the mode of `dotnet test`, the package source, the version, the code style | Exist |
| `BannedSymbols.txt`, `CodeAnalysis.globalconfig`, `.gitattributes` | The symbols no project may use, the analyzer options, the line endings | Exist |

## Projects of the host

The host is five projects, with Windows at the edge
([ADR 0011](../adr/0011-host-in-four-projects-with-windows-at-the-edge.md),
[ADR 0020](../adr/0020-windows-installer-packages-built-with-wix-which-run-a-program-of-the-host.md)):

| Project | Target | Content |
| --- | --- | --- |
| `Bravo.Core` | `net10.0` | Use cases, model, and the interfaces toward everything outside |
| `Bravo.Api` | `net10.0` | The contract exposed to the user interface and to the Visual Studio Code extension |
| `Bravo.Platform.Windows` | `net10.0-windows` | Adapters with no window: policies, credentials, the local Power BI Desktop |
| `Bravo.Desktop` | `net10.0-windows` | Window, webview, dialogs, composition; its executable is `Bravo.exe` |
| `Bravo.Setup` | `net10.0-windows` | What the installer packages run; its executable is `Bravo.Setup.exe` |

`Bravo.Api` and `Bravo.Platform.Windows` reference only `Bravo.Core`; `Bravo.Desktop` references all three and
is the only composition root, on the .NET Generic Host. `Bravo.Core` and `Bravo.Api` use no Windows API:
dialogs, the window and the UI thread are interfaces of `Bravo.Core` that the shell implements. No state is
static or ambient: every service is injected. `Bravo.Setup` references no project and none references it: it
is a program of its own, which the [installer](installer.md) carries.

The entry point, the host, the API and the main window with its webview exist.

### In the code

The host is five projects, with Windows at the edge
([ADR 0011](../adr/0011-host-in-four-projects-with-windows-at-the-edge.md),
[ADR 0020](../adr/0020-windows-installer-packages-built-with-wix-which-run-a-program-of-the-host.md)):

| Project | Target | What goes there | May reference |
| --- | --- | --- | --- |
| `Bravo.Core` | `net10.0` | Use cases, model, and the interfaces toward everything outside | Nothing |
| `Bravo.Api` | `net10.0` | The contract exposed to the user interface and to the Visual Studio Code extension | `Bravo.Core` |
| `Bravo.Platform.Windows` | `net10.0-windows` | Adapters with no window: policies, credentials, the local Power BI Desktop | `Bravo.Core` |
| `Bravo.Desktop` | `net10.0-windows` | Window, webview, dialogs, composition; its executable is `Bravo.exe` | The first three |
| `Bravo.Setup` | `net10.0-windows` | What the installer packages run ([Installer](installer.md)); its executable is `Bravo.Setup.exe` | Nothing |

- `Bravo.Core` and `Bravo.Api` use no Windows API: the build reports one as CA1416. Code that needs Windows gets
  an interface in `Bravo.Core` and its implementation in `Bravo.Platform.Windows`, or in `Bravo.Desktop` when it
  needs the window or the UI thread.
- No state is static or ambient: a service is injected, and `Bravo.Desktop` is the only place where services are
  registered.
- The user interface reaches the host through an HTTP API served on loopback
  ([ADR 0012](../adr/0012-http-on-loopback-between-user-interface-and-host.md)), which `Bravo.Api` holds
  ([API](api.md)).
- `Bravo.Desktop` starts, keeps one instance, serves the API and shows the user interface in the webview of
  its main window ([Window and webview](window-and-webview.md)).

A project of `src/` gets its test project with its first test: a run in which no test ran fails.
