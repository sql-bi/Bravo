# Design

How the redesigned system works, one document for each part. Each component adds its part here when its ADR is
accepted. A document says first what was decided and how the part behaves, then, under "In the code", where
the part lives and the rules for changing it.

| Document | Content |
| --- | --- |
| [`architecture.md`](architecture.md) | The layout of the repository and the projects of the host |
| [`build.md`](build.md) | The rules that the build and the tests enforce, and where each one lives |
| [`continuous-integration.md`](continuous-integration.md) | The workflow of GitHub Actions: its jobs and what fails each one |
| [`startup.md`](startup.md) | The order of the startup, the lifetime of the host, the single instance |
| [`window-and-webview.md`](window-and-webview.md) | The main window and its webview |
| [`api.md`](api.md) | The HTTP API through which the user interface reaches the host |
| [`user-interface-tests.md`](user-interface-tests.md) | The two kinds of tests of the user interface and what each proves |
| [`installer.md`](installer.md) | The Windows Installer packages and the program that they run |
| [`release.md`](release.md) | The version of a build, the workflow that makes a release, its six files, their signature, the update feed |
| [`updates.md`](updates.md) | How the application learns of a newer release, the file of each copy, the links that open in the browser |
| [`settings.md`](settings.md) | The settings of the user: the file of 1.0.x, how it is read and replaced |

## The previous implementation

[`previous/`](previous/) holds three documents on the previous implementation, the one on `main`: the paths
they name exist there. On this branch they are requirements and prior art for the component that replaces each
part.

| Document | Content |
| --- | --- |
| [`architecture.md`](previous/architecture.md) | Composition and ownership of resources, the threads of the user interface and of the server, the contract between the frontend and the API, the single instance, the boundaries of the configuration |
| [`diagnostics.md`](previous/diagnostics.md) | How the host reports exceptions: the paths of a report, startup and fatal errors, handled exceptions, the local log and the diagnostics pane, the consent to telemetry |
| [`versioning.md`](previous/versioning.md) | The single version computed by Nerdbank.GitVersioning: its source, the values derived from it, the rules |
