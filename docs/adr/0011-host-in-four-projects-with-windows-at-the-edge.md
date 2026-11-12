# 0011. The host is four projects, with Windows at the edge

- Status: Accepted
- Date: 2026-11-12

## Context

The host is written again from an empty `src/` ([ADR 0007](0007-redesign-on-a-development-branch.md)). Coding
agents write it, so the compiler and the tests must keep the boundaries between its parts
([ADR 0009](0009-rules-enforced-by-the-build.md)). Bravo runs on Windows, but its use cases need no Windows
API.

## Decision

| Project | Target | Content |
| --- | --- | --- |
| `Bravo.Core` | `net10.0` | Use cases, model, and the interfaces toward everything outside |
| `Bravo.Api` | `net10.0` | The contract exposed to the user interface and to the Visual Studio Code extension |
| `Bravo.Platform.Windows` | `net10.0-windows` | Adapters with no window: policies, credentials, the local Power BI Desktop |
| `Bravo.Desktop` | `net10.0-windows` | Window, webview, dialogs, composition; its executable is `Bravo` |

- `Api` and `Platform.Windows` reference only `Core`; `Desktop` references all three.
- No Windows in `Core` or `Api`: dialogs, the window and the UI thread are interfaces of `Core` that the shell
  implements. A portable project cannot reference a Windows one, and the platform compatibility analyzer
  reports a Windows-only API in portable code.
- No static or ambient state: what the host on `main` keeps in static classes is an injected service here.
- One composition root, in `Desktop`, on the .NET Generic Host as Microsoft documents it for Windows Forms.

References: Microsoft Learn
([Generic Host in Windows Forms](https://learn.microsoft.com/dotnet/desktop/winforms/advanced/how-to-use-host-builder),
[platform compatibility analyzer](https://learn.microsoft.com/dotnet/standard/analyzers/platform-compat-analyzer));
Blazor Hybrid, whose webview core names no operating system in its target (`dotnet/aspnetcore`,
`src/Components/WebView/WebView/src/WebViewManager.cs`), while its Windows Forms and WPF shells target
`-windows` (`dotnet/maui`, `src/BlazorWebView/src/`).

## Rejected alternatives

- Two projects, portable and Windows: the contract with the user interface would live inside the core.
- One project per feature: premature while no feature exists.
- One target framework with platform checks at run time, as `git-ecosystem/git-credential-manager` does in
  `src/Core/CommandContext.cs`: the compiler would no longer keep Windows out of the core.

## Consequences

- The architecture tests state the graph above in their table of allowed references.
- The portable projects are built and tested on Linux as well; the Windows ones on Windows only.
- A feature that needs Windows gets an interface in `Core`, implemented in `Platform.Windows` or `Desktop`.
- Startup, the contract and the window are designed by backlog H-02, H-03 and H-04.
